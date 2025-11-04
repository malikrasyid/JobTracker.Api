using System.Collections.Generic;

namespace JobTracker.Api.Config
{
    public static class DefaultPipeline
    {
        // Use a fixed, valid 24-character ObjectId for consistency across all users
        public static readonly string Id = "000000000000000000000001";

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
