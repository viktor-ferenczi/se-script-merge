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

            var segment = new ISegment<IMyTerminalBlock>
            {
                Next = Me,
            };

            ISegment<IMyTerminalBlock> otherSegment = segment;
            Echo(otherSegment.Next.CustomName);
        }

        // ReSharper disable once UnusedMember.Global
        public void Save()
        {
            var a0 = "\n";
            var a1 = "\n";
            var a2 = "\n";
            var a3 = "\n";
            var a4 = "\n";
            var a5 = "\n";
            var a6 = "\n";
            var a7 = "\n";
            var a8 = "\n";
            var a9 = "\n";

            var s = a0 + a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9;

            var _ = new[]
            {
                "\n", "\n", "\n", "\n", "\n", "\n", "\n", "\n", "\n", "\n",
                "\n", "\n", "\n", "\n", "\n", "\n", "\n", "\n", "\n", "\n",
            };
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
            var _ = e.ToString();

            return new Support();
        }
    }

    enum CustomEnum
    {
        First,
        Second,
    }
}