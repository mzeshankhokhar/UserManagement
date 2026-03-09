namespace UserManagement.Core.Model
{
    /// <summary>
    /// Tracks database schema versions for migration management
    /// </summary>
    public class DatabaseVersion : BaseEntity
    {
        public string Version { get; set; }
        public string MigrationName { get; set; }
        public string Description { get; set; }
        public DateTime AppliedOn { get; set; }
        public string AppliedBy { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
