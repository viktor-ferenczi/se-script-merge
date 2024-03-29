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


namespace DebugMergedScript
{
    class Program : MyGridProgram
    {
        #region MergedScript

public class ISegment<T>
{
    public T Next;
}

public interface ISupport: IComparable<int>
{
    bool IsGood();
    bool MustImplementThis(bool b);
    bool Good { get; set; }
}

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

    Echo("DEBUG");


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


enum CustomEnum
{
    First,
    Second,
}

public class Support : ISupport
{
    public bool Good { get; set; }
    public bool IsGood() => Good;

    public Support(bool init = true)
    {
        Good = init;
    }

    public bool MustImplementThis(bool b) => !b;

    public int CompareTo(int other)
    {
        return Good ? other : -other;
    }

    public override string ToString()
    {
        return $"Support({Good})";
    }

    // Finalizers are blacklisted in SE
    // ~Support()
    // {
    //     good = false;
    // }
}

public class SupportExplicit : ISupport
{
    int IComparable<int>.CompareTo(int other)
    {
        return 0;
    }

    bool ISupport.IsGood()
    {
        return false;
    }

    bool ISupport.MustImplementThis(bool b)
    {
        return true;
    }

    bool ISupport.Good { get; set; }
}


        #endregion
    }
}