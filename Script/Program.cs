// ReSharper disable ConvertConstructorToMemberInitializers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable RedundantUsingDirective
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
// ReSharper disable CheckNamespace

// Import everything available for PB scripts in-game
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;


namespace Script
{
    // ReSharper disable once UnusedType.Global
    class Program : MyGridProgram
    {
        //! support
        private static int staticVar;
        private int instanceVar;
        private Support support;

        public bool IsFunctional { get; set; } = true;

        public Program()
        {
            instanceVar = 2;
        }

        // ReSharper disable once UnusedMember.Global
        public void Main(string argument, UpdateType updateSource)
        {
            // TODO

            #region "Do not remove this"

            Echo("REGION 1");

            #endregion

#if DEBUG
            Echo("DEBUG");
#endif

#if !DEBUG
            Echo("RELEASE");
#endif

            #region "Do not remove that"

            Echo("REGION 2");

            #endregion

            support = CustomMethod();
        }

        // ReSharper disable once UnusedMember.Global
        public void Save()
        {
            // TODO
        }

        private Support CustomMethod()
        {
            var localVar = 1;
            localVar += 2;
            staticVar++;
            instanceVar -= staticVar;
            localVar += instanceVar;
            Echo(localVar.ToString());

            CustomEnum e = CustomEnum.First;
            e = CustomEnum.Second;

            return new Support();
        }
    }

    enum CustomEnum
    {
        First,
        Second,
    }
}