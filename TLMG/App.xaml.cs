using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace TLMG
{
    public partial class App : Application
    {
        public App()
        {
            LanguageManager.LanguageChanged += OnLanguageChanged;
            LanguageManager.InitializeLanguage();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            string language = LanguageManager.CurrentCulture;
            Collection<ResourceDictionary> mergedDictionaries = Resources.MergedDictionaries;

            for (int dictionaryIndex = mergedDictionaries.Count - 1; dictionaryIndex >= 0; dictionaryIndex--)
            {
                ResourceDictionary currentDictionary = mergedDictionaries[dictionaryIndex];
                if (currentDictionary.Source != null && currentDictionary.Source.OriginalString.Contains("Strings."))
                {
                    mergedDictionaries.RemoveAt(dictionaryIndex);
                }
            }

            ResourceDictionary newDictionary = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/TLMG;component/Languages/Strings.{language}.xaml")
            };
            mergedDictionaries.Add(newDictionary);

            foreach (Window activeWindow in Windows)
            {
                object currentContent = activeWindow.Content;
                activeWindow.Content = null;
                activeWindow.Content = currentContent;
            }
        }
    }
}