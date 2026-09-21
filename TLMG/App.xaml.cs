using System;
using System.Linq;
using System.Windows;

namespace TLMG
{
    public partial class App : Application
    {
        public App()
        {
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            string language = LanguageManager.CurrentCulture;
            var merged = Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var dictionary = merged[i];
                if (dictionary.Source != null && dictionary.Source.OriginalString.StartsWith("Resources/StringResources."))
                {
                    merged.RemoveAt(i);
                }
            }

            var newDictionary = new ResourceDictionary
            {
                Source = new Uri($"Resources/StringResources.{language}.xaml", UriKind.Relative)
            };
            merged.Add(newDictionary);

            foreach (Window window in Windows)
            {
                var content = window.Content;
                window.Content = null;
                window.Content = content;
            }
        }
    }
}