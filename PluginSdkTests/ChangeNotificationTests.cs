using System.Collections.Generic;
using System.ComponentModel;
using PluginSdk.Config;
using PluginSdk.Tools;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Tests for <see cref="PluginConfig"/> change notification, including the
    /// documented "mutate in place, then call <see cref="PluginConfig.NotifyChanged"/>"
    /// rule for lists, dictionaries and structs.
    /// </summary>
    public class ChangeNotificationTests
    {
        private static List<string> CaptureChanges(INotifyPropertyChanged config)
        {
            var changed = new List<string>();
            config.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            return changed;
        }

        [Fact]
        public void Bool_SettingDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.Flag = true;
            Assert.Equal(new[] { nameof(TestConfig.Flag) }, changed);
        }

        [Fact]
        public void Bool_SettingSameValue_DoesNotRaiseEvent()
        {
            var c = new TestConfig { Flag = true };
            var changed = CaptureChanges(c);
            c.Flag = true;
            Assert.Empty(changed);
        }

        [Fact]
        public void Int_SettingDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.Integer = 42;
            Assert.Equal(new[] { nameof(TestConfig.Integer) }, changed);
        }

        [Fact]
        public void Long_SettingDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.LongInteger = 1L << 40;
            Assert.Equal(new[] { nameof(TestConfig.LongInteger) }, changed);
        }

        [Fact]
        public void Float_SettingDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.FloatNumber = 0.5f;
            Assert.Equal(new[] { nameof(TestConfig.FloatNumber) }, changed);
        }

        [Fact]
        public void Double_SettingDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.DoubleNumber = 3.14;
            Assert.Equal(new[] { nameof(TestConfig.DoubleNumber) }, changed);
        }

        [Fact]
        public void String_SettingDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.Text = "hello";
            Assert.Equal(new[] { nameof(TestConfig.Text) }, changed);
        }

        [Fact]
        public void String_SettingSameValue_DoesNotRaiseEvent()
        {
            var c = new TestConfig { Text = "hello" };
            var changed = CaptureChanges(c);
            c.Text = "hello";
            Assert.Empty(changed);
        }

        [Fact]
        public void List_Reassignment_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.IntList = new List<int> { 1, 2, 3 };
            Assert.Equal(new[] { nameof(TestConfig.IntList) }, changed);
        }

        [Fact]
        public void List_InPlaceMutation_DoesNotRaiseEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            // The documented gotcha: mutating the same list instance is
            // invisible to the change-notification system.
            c.IntList.Add(1);
            c.IntList.Add(2);

            Assert.Empty(changed);
            Assert.Equal(new[] { 1, 2 }, c.IntList);
        }

        [Fact]
        public void List_DocumentedMutationPattern_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            // Documented pattern: mutate in place, then notify explicitly.
            c.IntList.Add(7);
            c.NotifyChanged(nameof(c.IntList));

            Assert.Equal(new[] { nameof(TestConfig.IntList) }, changed);
            Assert.Equal(new[] { 7 }, c.IntList);
        }

        [Fact]
        public void Dict_Reassignment_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);
            c.DictStringInt = new SerializableDictionary<string, int> { ["k"] = 1 };
            Assert.Equal(new[] { nameof(TestConfig.DictStringInt) }, changed);
        }

        [Fact]
        public void Dict_InPlaceMutation_DoesNotRaiseEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            c.DictStringInt["k"] = 1;
            c.DictStringInt["k"] = 2;

            Assert.Empty(changed);
            Assert.Equal(2, c.DictStringInt["k"]);
        }

        [Fact]
        public void Dict_DocumentedMutationPattern_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            // Documented pattern: mutate in place, then notify explicitly.
            c.DictStringInt["k"] = 1;
            c.NotifyChanged(nameof(c.DictStringInt));

            Assert.Equal(new[] { nameof(TestConfig.DictStringInt) }, changed);
            Assert.Equal(1, c.DictStringInt["k"]);
        }

        [Fact]
        public void Struct_Reassignment_WithDifferentValue_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            var s = c.StructValue;
            s.Integer = 5;
            c.StructValue = s;

            Assert.Equal(new[] { nameof(TestConfig.StructValue) }, changed);
            Assert.Equal(5, c.StructValue.Integer);
        }

        [Fact]
        public void Struct_Reassignment_WithSameValue_DoesNotRaiseEvent()
        {
            var c = new TestConfig { StructValue = new TestStruct { Integer = 5, Text = "x" } };
            var changed = CaptureChanges(c);

            c.StructValue = new TestStruct { Integer = 5, Text = "x" };

            Assert.Empty(changed);
        }

        [Fact]
        public void ListOfStruct_Reassignment_RaisesEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            c.StructList = new List<TestStruct> { new TestStruct { Integer = 1 } };

            Assert.Equal(new[] { nameof(TestConfig.StructList) }, changed);
        }

        [Fact]
        public void ListOfStruct_InPlaceAdd_DoesNotRaiseEvent()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            c.StructList.Add(new TestStruct { Integer = 1 });

            Assert.Empty(changed);
            Assert.Single(c.StructList);
        }

        [Fact]
        public void NotifyChanged_AfterInPlaceMutation_RaisesEventForNamedProperty()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            c.StructList.Add(new TestStruct { Integer = 1 });
            c.NotifyChanged(nameof(c.StructList));

            Assert.Equal(new[] { nameof(TestConfig.StructList) }, changed);
            Assert.Single(c.StructList);
        }

        [Fact]
        public void NotifyChanged_NotifiesUnconditionally_EvenWithoutAChange()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            // No equality gate: NotifyChanged always raises the event.
            c.NotifyChanged(nameof(c.Integer));

            Assert.Equal(new[] { nameof(TestConfig.Integer) }, changed);
        }

        [Fact]
        public void NotifyChanged_AfterNestedInPlaceMutation_RaisesEventForTopLevelProperty()
        {
            var c = new TestConfig();
            var changed = CaptureChanges(c);

            // The struct copy shares the inner list reference, so an in-place
            // edit reaches the option; one top-level notification suffices.
            c.Nested.Numbers.Add(42);
            c.NotifyChanged(nameof(c.Nested));

            Assert.Equal(new[] { nameof(TestConfig.Nested) }, changed);
            Assert.Equal(new[] { 42 }, c.Nested.Numbers);
        }
    }
}
