using System.Collections.Generic;

namespace JobTracker.Api.Config
{
    public static class DefaultPipeline
    {
        public static readonly string Id = "default";
        public static readonly string Name = "Default Pipeline";
        public static readonly List<string> Stages = new List<string>
        {
            "Wishlist",
            "Applied",
            "Screening",
            "Interview",
            "Offer",
            "Rejected"
        };
    }
}
