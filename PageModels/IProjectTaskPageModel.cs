using BRTDataTools.App.Models;
using CommunityToolkit.Mvvm.Input;

namespace BRTDataTools.App.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}