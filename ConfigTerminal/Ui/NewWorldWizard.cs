using System;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// New-world creation by folder copy (§2.7 / §9.6): pick a template, name the
/// world, then copy the template into <c>Saves/&lt;name&gt;</c> and stamp the name
/// into its <c>Sandbox_config.sbc</c> (see <see cref="WorldCreator"/>). No server
/// start — the world exists and is editable immediately, and is activated so the
/// DS loads it next.
/// </summary>
internal static class NewWorldWizard
{
    public static void Run(AppShell shell)
    {
        DsInstance instance = shell.Instance;
        if (instance.Templates.Templates.Count == 0)
        {
            Dialogs.Error("New World", "No world templates found. A DS install (Content/CustomWorlds) is required; set it with -ds64.");
            return;
        }

        WorldTemplate template = PickTemplate(instance);
        if (template == null)
            return;

        string name = PromptName(template, instance);
        if (string.IsNullOrWhiteSpace(name))
            return;

        string question = $"Create world '{name}' from template '{template.DisplayName}'?";
        string details =
            "Copies the template into Saves/ (no server start):\n" +
            $"  • Saves/{name}/  ← copy of '{template.FolderName}'\n" +
            $"  • Sandbox_config.sbc SessionName ← {name}\n" +
            "  • activated (LastSession.sbl) so the DS loads it next\n\n" +
            "The world appears immediately and can be edited before first start.";

        if (!Dialogs.ConfirmDetails("Create world", question, details, "Create", "Cancel"))
            return;

        string savesPath = instance.Binding.SavesPath;
        Dialogs.RunBackground(
            () => WorldCreator.CreateFromTemplate(template, name, savesPath),
            _ =>
            {
                shell.ReloadInstance();
                ActivateCreatedWorld(shell, name);
                shell.ShowWorlds(); // rebuild the Worlds list so the new world is visible immediately
                Dialogs.Info("World created",
                    $"'{name}' was created under Saves/ and activated.\n" +
                    "Edit it under Worlds, or start the server to run it.");
            });
    }

    /// <summary>Points the DS at the freshly created world (LastSession.sbl), mirroring WorldsView.ActivateWorld.</summary>
    private static void ActivateCreatedWorld(AppShell shell, string name)
    {
        try
        {
            WorldInfo world = shell.Instance.Worlds.Find(name);
            if (world == null)
                return; // copy reported success but the folder isn't visible — leave selection untouched

            LastSessionFile.ForWorld(world, shell.Instance.Binding.SavesPath)
                .Write(shell.Writer, LastSessionFile.PathFor(shell.Instance.Binding.SavesPath));

            DedicatedConfigDocument cfg = shell.Instance.Config;
            if (cfg != null)
            {
                cfg.IgnoreLastSession = false;
                cfg.LoadWorld = string.Empty;
                cfg.PremadeCheckpointPath = string.Empty; // clear any leftover staging from an earlier attempt
                cfg.Save(shell.Writer);
            }
            shell.ReloadInstance();
        }
        catch (Exception e)
        {
            Dialogs.Error("Activate failed",
                $"The world was created but could not be activated: {e.Message}\n" +
                "Activate it manually under Worlds (F5).");
        }
    }

    private static WorldTemplate PickTemplate(DsInstance instance)
    {
        var dlg = new Dialog("Pick a template", 60, 18) { ColorScheme = TurboVisionTheme.Dialog };
        var list = new ListView(instance.Templates.Templates.Select(t => $"{t.DisplayName}   ({t.FolderName})").ToList())
        {
            X = 1, Y = 1, Width = Dim.Fill(2), Height = Dim.Fill(3), ColorScheme = TurboVisionTheme.Dialog,
        };
        WorldTemplate result = null;
        var ok = new Button("Select", true);
        ok.Clicked += () =>
        {
            int i = list.SelectedItem;
            if (i >= 0 && i < instance.Templates.Templates.Count)
                result = instance.Templates.Templates[i];
            Application.RequestStop(dlg);
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.Add(new Label("Templates from the DS install (Content/CustomWorlds):") { X = 1, Y = 0 }, list);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        list.SetFocus();
        Application.Run(dlg);
        return result;
    }

    private static string PromptName(WorldTemplate template, DsInstance instance)
    {
        var dlg = new Dialog("World name", 56, 10) { ColorScheme = TurboVisionTheme.Dialog };
        var field = new TextField(template.DisplayName) { X = 1, Y = 2, Width = Dim.Fill(2) };
        var warn = new Label("") { X = 1, Y = 4, Width = Dim.Fill(2), ColorScheme = TurboVisionTheme.Dialog };
        string result = null;

        void Validate()
        {
            string n = field.Text.ToString();
            if (n.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                warn.Text = "Name must not contain / \\ or :";
            else if (instance.Worlds.Worlds.Any(w =>
                         string.Equals(w.SessionName, n, StringComparison.OrdinalIgnoreCase)
                         || Io.PlatformPaths.PathComparer.Equals(w.FolderName, n)))
                warn.Text = "A world with this name already exists.";
            else
                warn.Text = string.Empty;
        }
        field.TextChanged += _ => Validate();

        var ok = new Button("OK", true);
        ok.Clicked += () =>
        {
            string n = field.Text.ToString().Trim();
            if (string.IsNullOrEmpty(n) || n.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                Dialogs.Error("World name", "Enter a valid name (no / \\ or :).");
                return;
            }
            result = n;
            Application.RequestStop(dlg);
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.Add(new Label("World name:") { X = 1, Y = 1 }, field, warn);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        Validate();
        field.SetFocus();
        Application.Run(dlg);
        return result;
    }
}
