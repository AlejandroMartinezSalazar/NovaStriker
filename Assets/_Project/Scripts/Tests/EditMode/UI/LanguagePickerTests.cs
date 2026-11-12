using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Infrastructure.Localization;
using NovaStriker.UI.Localization;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.UI
{
    public class LanguagePickerTests
    {
        private sealed class FakePickerView : ILanguagePickerView
        {
            public event Action<Language> LanguageSelected;
            public readonly List<string> Calls = new List<string>();
            public void Show(Language preselected) => Calls.Add($"Show:{preselected}");
            public void Hide() => Calls.Add("Hide");
            public void Pick(Language language) => LanguageSelected?.Invoke(language);
            public bool HasListeners => LanguageSelected != null;
        }

        [Test]
        public void FirstLaunch_ShowsPicker_WithDeviceLanguage_AndPickingClosesIt()
        {
            var settings = new MemorySettings();
            var localization = new LocalizationService(settings, Language.English);
            var view = new FakePickerView();

            var presenter = new LanguagePickerPresenter(view, localization);
            view.Pick(Language.Spanish);
            presenter.Dispose();

            CollectionAssert.AreEqual(new[] { "Show:English", "Hide" }, view.Calls);
            Assert.AreEqual(Language.Spanish, settings.Language);
            Assert.AreEqual("JUGAR", localization.Get(TextKey.MenuPlay));
            Assert.IsFalse(view.HasListeners);
        }

        [Test]
        public void LanguageAlreadyChosen_NeverShowsPicker()
        {
            var settings = new MemorySettings();
            settings.SetLanguage(Language.Portuguese);
            var view = new FakePickerView();

            using (new LanguagePickerPresenter(view, new LocalizationService(settings, Language.English)))
            {
                CollectionAssert.AreEqual(new[] { "Hide" }, view.Calls);
            }
        }
    }
}
