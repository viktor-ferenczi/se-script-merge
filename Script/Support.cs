namespace Script
{
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
}