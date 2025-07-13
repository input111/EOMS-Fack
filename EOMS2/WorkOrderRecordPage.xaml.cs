using System.Collections.ObjectModel;
using System.Text.Json;

namespace EOMS2
{
    public partial class WorkOrderRecordPage : ContentPage
    {
        public ObservableCollection<WorkOrder> WorkOrders { get; set; }
        private const string WORK_ORDERS_KEY = "work_orders";
        public Command<WorkOrder> DeleteCommand { get; }
        public bool IsEmpty => WorkOrders.Count == 0;

        public WorkOrderRecordPage()
        {
            InitializeComponent();
            WorkOrders = new ObservableCollection<WorkOrder>();
            DeleteCommand = new Command<WorkOrder>(DeleteWorkOrder);
            workOrdersCollection.ItemsSource = WorkOrders;
            BindingContext = this;
        }

        private async void DeleteWorkOrder(WorkOrder workOrder)
        {
            bool answer = await DisplayAlert("确认", "确定要删除这条记录吗？？", "是", "否");
            if (answer)
            {
                WorkOrders.Remove(workOrder);
                SaveWorkOrders();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        private void SaveWorkOrders()
        {
            try
            {
                string updatedJson = JsonSerializer.Serialize(WorkOrders.ToList());
                Preferences.Set(WORK_ORDERS_KEY, updatedJson);
            }
            catch (Exception ex)
            {
                DisplayAlert("错误", "保存更改失败: " + ex.Message, "确定");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // 无论是输入还是删除文本，都触发筛选
            ApplyFilter();
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnResetFilter(object sender, EventArgs e)
        {
            WorkOrderSearchEntry.Text = string.Empty;
            PonPortSearchEntry.Text = string.Empty;
            StartDatePicker.Date = DateTime.Today.AddDays(-30);
            EndDatePicker.Date = DateTime.Today;
            LoadWorkOrders(); // 重新加载所有工单
        }

        private void ApplyFilter()
        {
            var filteredList = WorkOrders.ToList(); // 先获取所有工单

            // 按工单号筛选
            if (!string.IsNullOrWhiteSpace(WorkOrderSearchEntry.Text))
            {
                filteredList = filteredList.Where(wo =>
                    wo.WorkOrderNumber.Contains(WorkOrderSearchEntry.Text, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 按PON口筛选
            if (!string.IsNullOrWhiteSpace(PonPortSearchEntry.Text))
            {
                filteredList = filteredList.Where(wo =>
                    wo.PonPort.Contains(PonPortSearchEntry.Text, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 按日期筛选
            if (StartDatePicker.Date <= EndDatePicker.Date)
            {
                filteredList = filteredList.Where(wo =>
                {
                    if (DateTime.TryParse(wo.CompletionTime, out DateTime completionDate))
                    {
                        return completionDate.Date >= StartDatePicker.Date &&
                               completionDate.Date <= EndDatePicker.Date;
                    }
                    return false;
                }).ToList();
            }

            // 清空并重新添加筛选后的结果
            WorkOrders.Clear();
            foreach (var order in filteredList.OrderByDescending(o => o.CompletionTime))
            {
                WorkOrders.Add(order);
            }
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void LoadWorkOrders()
        {
            try
            {
                string jsonData = Preferences.Get(WORK_ORDERS_KEY, string.Empty);
                WorkOrders.Clear();

                if (!string.IsNullOrEmpty(jsonData))
                {
                    var savedOrders = JsonSerializer.Deserialize<List<WorkOrder>>(jsonData)
                        .OrderByDescending(o => o.CompletionTime); // 倒序排列
                    foreach (var order in savedOrders)
                    {
                        WorkOrders.Add(order);
                    }
                }
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                DisplayAlert("错误", "加载工单记录失败: " + ex.Message, "确定");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadWorkOrders();
        }

        public static void SaveWorkOrder(string workOrderNumber, string ponPort)
        {
            try
            {
                string jsonData = Preferences.Get(WORK_ORDERS_KEY, string.Empty);
                List<WorkOrder> workOrders;

                if (string.IsNullOrEmpty(jsonData))
                {
                    workOrders = new List<WorkOrder>();
                }
                else
                {
                    workOrders = JsonSerializer.Deserialize<List<WorkOrder>>(jsonData);
                }

                // 在列表开头插入新工单
                workOrders.Insert(0, new WorkOrder
                {
                    WorkOrderNumber = workOrderNumber,
                    CompletionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PonPort = ponPort
                });

                string updatedJson = JsonSerializer.Serialize(workOrders);
                Preferences.Set(WORK_ORDERS_KEY, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存工单时出错: {ex.Message}");
            }
        }
    }

    public class WorkOrder
    {
        public string WorkOrderNumber { get; set; }
        public string CompletionTime { get; set; }
        public string PonPort { get; set; }
    }
}