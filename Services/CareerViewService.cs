using Umbraco.Cms.Core.Services;

namespace Centrocdx.Services;

public interface ICareerViewService
{
    int IncrementAndGet(Guid jobKey, bool alreadyCountedInSession);
    int GetCount(Guid jobKey);
}

public class CareerViewService : ICareerViewService
{
    private readonly IContentService _contentService;
    private readonly object _lock = new();

    public CareerViewService(IContentService contentService)
    {
        _contentService = contentService;
    }

    public int GetCount(Guid jobKey)
    {
        var content = _contentService.GetById(jobKey);
        if (content == null)
        {
            return 0;
        }

        return content.GetValue<int?>("viewCount") ?? 0;
    }

    public int IncrementAndGet(Guid jobKey, bool alreadyCountedInSession)
    {
        lock (_lock)
        {
            var content = _contentService.GetById(jobKey);
            if (content == null)
            {
                return 0;
            }

            var current = content.GetValue<int?>("viewCount") ?? 0;
            if (alreadyCountedInSession)
            {
                return current;
            }

            current++;
            content.SetValue("viewCount", current);
            _contentService.Save(content);
            return current;
        }
    }
}
