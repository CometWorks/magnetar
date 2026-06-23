namespace PluginSdk
{
    /// <summary>
    /// Text payload displayed by the Magnetar client mod through Space
    /// Engineers' mission-screen popup.
    /// </summary>
    public readonly struct MissionScreenContent
    {
        public string ScreenTitle { get; }
        public string CurrentObjectivePrefix { get; }
        public string CurrentObjective { get; }
        public string ScreenDescription { get; }
        public string OkButtonCaption { get; }

        public bool HasContent =>
            !string.IsNullOrEmpty(ScreenTitle) ||
            !string.IsNullOrEmpty(CurrentObjectivePrefix) ||
            !string.IsNullOrEmpty(CurrentObjective) ||
            !string.IsNullOrEmpty(ScreenDescription);

        public MissionScreenContent(
            string screenTitle,
            string currentObjectivePrefix,
            string currentObjective,
            string screenDescription,
            string okButtonCaption = null)
        {
            ScreenTitle = screenTitle;
            CurrentObjectivePrefix = currentObjectivePrefix;
            CurrentObjective = currentObjective;
            ScreenDescription = screenDescription;
            OkButtonCaption = okButtonCaption;
        }
    }
}
