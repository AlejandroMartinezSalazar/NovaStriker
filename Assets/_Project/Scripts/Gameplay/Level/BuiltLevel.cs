using System;
using System.Collections.Generic;
using NovaStriker.Gameplay.Characters;

namespace NovaStriker.Gameplay.Level
{
    /// <summary>Un nivel ya instanciado: su escenario y los futbolistas creados.</summary>
    public sealed class BuiltLevel
    {
        public BuiltLevel(LevelDefinition definition, LevelLayout layout, IReadOnlyList<FootballerController> characters)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        public LevelDefinition Definition { get; }
        public LevelLayout Layout { get; }
        public IReadOnlyList<FootballerController> Characters { get; }
    }
}
