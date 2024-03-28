using System;

namespace Script
{
    public interface ISupport: IComparable<int>
    {
        bool IsGood();
        bool MustImplementThis(bool b);
        bool Good { get; set; }
    }
}