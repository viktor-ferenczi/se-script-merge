using System;

namespace Script
{
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
}