namespace OnlineLearningPlatformApi.Application.Requests.Module
{
    public class UpdateModuleRequest
    {
        public Guid ModuleId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Index { get; set; }
    }
}
