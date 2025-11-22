using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Provides helper methods for sequencing screenplay elements based on industry expectations.
    /// </summary>
    public class ScreenplayStructureAdvisor
    {
        public IReadOnlyList<ScriptElementType> GetPreferredNextElements(ScriptElementType elementType)
        {
            var profile = ScreenplayElementProfiles.GetProfile(elementType);
            return profile.PreferredNext;
        }

        public ScriptElementType? GetDefaultNextElement(ScriptElementType elementType)
        {
            var preferred = GetPreferredNextElements(elementType);
            return preferred.FirstOrDefault();
        }
    }
}
