using System;
using System.Collections.Generic;

namespace TLMG.Services.Utilities.Languages
{
    public class EmailLanguageStrategyFactory
    {
        private readonly Dictionary<string, IEmailLanguageStrategy> _strategies;
        private readonly IEmailLanguageStrategy _defaultStrategy;

        public EmailLanguageStrategyFactory()
        {
            _strategies = new Dictionary<string, IEmailLanguageStrategy>(StringComparer.OrdinalIgnoreCase);

            SpanishEmailLanguageStrategy spanishStrategy = new SpanishEmailLanguageStrategy();
            EnglishEmailLanguageStrategy englishStrategy = new EnglishEmailLanguageStrategy();

            _strategies.Add(spanishStrategy.CulturePrefix, spanishStrategy);
            _strategies.Add(englishStrategy.CulturePrefix, englishStrategy);

            _defaultStrategy = spanishStrategy;
        }

        public IEmailLanguageStrategy GetStrategy(string cultureCode)
        {
            IEmailLanguageStrategy selectedStrategy = _defaultStrategy;

            if (!string.IsNullOrWhiteSpace(cultureCode))
            {
                foreach (KeyValuePair<string, IEmailLanguageStrategy> entry in _strategies)
                {
                    if (cultureCode.StartsWith(entry.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedStrategy = entry.Value;
                        break;
                    }
                }
            }

            return selectedStrategy;
        }
    }
}