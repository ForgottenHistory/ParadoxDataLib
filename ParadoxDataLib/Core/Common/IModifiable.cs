using System.Collections.Generic;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Core.Common
{
    public interface IModifiable
    {
        List<Modifier> Modifiers { get; }
        void ApplyModifier(Modifier modifier);
        void RemoveModifier(string modifierId);
        void ClearModifiers();
    }
}