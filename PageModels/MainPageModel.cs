using BRTDataTools.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;

namespace BRTDataTools.App.PageModels
{
    public partial class MainPageModel : ObservableObject, IProjectTaskPageModel
    {
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskRepository _taskRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly ModalErrorHandler _errorHandler;
        private readonly SeedDataService _seedDataService;

        public LineChart Chart { get; set; }

        [ObservableProperty]
        private List<CategoryChartData> _todoCategoryData = [];

        [ObservableProperty]
        private List<Brush> _todoCategoryColors = [];

        [ObservableProperty]
        private List<ProjectTask> _tasks = [];

        [ObservableProperty]
        private List<Project> _projects = [];

        [ObservableProperty]
        bool _isBusy;

        [ObservableProperty]
        bool _isRefreshing;

        [ObservableProperty]
        private string _today = DateTime.Now.ToString("dddd, MMM d");

        [ObservableProperty]
        private int _minChart1;
        [ObservableProperty]
        private int _maxChart1;
        [ObservableProperty]
        private int _avgChart1;

        public bool HasCompletedTasks
            => Tasks?.Any(t => t.IsCompleted) ?? false;

        public MainPageModel(SeedDataService seedDataService, ProjectRepository projectRepository,
            TaskRepository taskRepository, CategoryRepository categoryRepository, ModalErrorHandler errorHandler)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _categoryRepository = categoryRepository;
            _errorHandler = errorHandler;
            _seedDataService = seedDataService;
        }

        private async Task LoadData()
        {
            try
            {
                IsBusy = true;

                Projects = await _projectRepository.ListAsync();

                var chartData = new List<CategoryChartData>();
                var microChartData = new List<ChartEntry>();
                var chartColors = new List<Brush>();

                var categories = await _categoryRepository.ListAsync();
                //foreach (var category in categories)
                //{
                //    chartColors.Add(category.ColorBrush);

                //    var ps = Projects.Where(p => p.CategoryID == category.ID).ToList();
                //    int tasksCount = ps.SelectMany(p => p.Tasks).Count();

                //chartData.Add(new(category.Title, tasksCount));
                //}

                // Use HttpClient to get a comma delimited list of numbers from a web service
                HttpClient httpClient = new();
                httpClient.BaseAddress = new Uri("http://192.168.4.1");
                var response = await httpClient.GetAsync("/");
                // parse the content of the response
                var stream = await response.Content.ReadAsStringAsync();
                
                HashSet<int> plots = new();
                foreach (var plot in stream.Split(','))
                {
                    if (int.TryParse(plot, out int value))
                    {
                        plots.Add(value);
                        chartData.Add(new(DateTime.UtcNow.ToString(), value));
                        microChartData.Add(new ChartEntry(value)
                        {
                            Label = DateTime.UtcNow.ToString(),
                            ValueLabel = value.ToString(),
                        });
                    }
                }
                
                MinChart1 = chartData.Select(x => x.YValue).Min();
                MaxChart1 = chartData.Select(x => x.YValue).Max();
                AvgChart1 = (int)chartData.Select(x => x.YValue).Average();

                TodoCategoryData = chartData;
                TodoCategoryColors = chartColors;

                Chart = new LineChart() { Entries = microChartData };                

                Tasks = await _taskRepository.ListAsync();
            }
            catch(Exception ex)
            {

            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasCompletedTasks));
            }
        }

        private async Task InitData(SeedDataService seedDataService)
        {
            bool isSeeded = Preferences.Default.ContainsKey("is_seeded");

            if (!isSeeded)
            {
                await seedDataService.LoadSeedDataAsync();
            }

            Preferences.Default.Set("is_seeded", true);
            await Refresh();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsRefreshing = true;
                await LoadData();
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private void NavigatedTo() =>
            _isNavigatedTo = true;

        [RelayCommand]
        private void NavigatedFrom() =>
            _isNavigatedTo = false;

        [RelayCommand]
        private async Task Appearing()
        {
            if (!_dataLoaded)
            {
                await InitData(_seedDataService);
                _dataLoaded = true;
                await Refresh();
            }
            // This means we are being navigated to
            else if (!_isNavigatedTo)
            {
                await Refresh();
            }

           
        }

        [RelayCommand]
        private Task TaskCompleted(ProjectTask task)
        {
            OnPropertyChanged(nameof(HasCompletedTasks));
            return _taskRepository.SaveItemAsync(task);
        }

        [RelayCommand]
        private Task AddTask()
            => Shell.Current.GoToAsync($"task");

        [RelayCommand]
        private Task NavigateToProject(Project project)
            => Shell.Current.GoToAsync($"project?id={project.ID}");

        [RelayCommand]
        private Task NavigateToTask(ProjectTask task)
            => Shell.Current.GoToAsync($"task?id={task.ID}");

        [RelayCommand]
        private async Task CleanTasks()
        {
            var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();
            foreach (var task in completedTasks)
            {
                await _taskRepository.DeleteItemAsync(task);
                Tasks.Remove(task);
            }

            OnPropertyChanged(nameof(HasCompletedTasks));
            Tasks = new(Tasks);
            await AppShell.DisplayToastAsync("All cleaned up!");
        }
    }
}