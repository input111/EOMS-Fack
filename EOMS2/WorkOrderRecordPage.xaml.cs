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
            bool answer = await DisplayAlert("ȷ��", "ȷ��Ҫɾ��������¼�𣿣�", "��", "��");
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
                DisplayAlert("����", "�������ʧ��: " + ex.Message, "ȷ��");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // ���������뻹��ɾ���ı���������ɸѡ
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
            LoadWorkOrders(); // ���¼������й���
        }

        private void ApplyFilter()
        {
            var filteredList = WorkOrders.ToList(); // �Ȼ�ȡ���й���

            // ��������ɸѡ
            if (!string.IsNullOrWhiteSpace(WorkOrderSearchEntry.Text))
            {
                filteredList = filteredList.Where(wo =>
                    wo.WorkOrderNumber.Contains(WorkOrderSearchEntry.Text, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // ��PON��ɸѡ
            if (!string.IsNullOrWhiteSpace(PonPortSearchEntry.Text))
            {
                filteredList = filteredList.Where(wo =>
                    wo.PonPort.Contains(PonPortSearchEntry.Text, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // ������ɸѡ
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

            // ��ղ��������ɸѡ��Ľ��
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
                        .OrderByDescending(o => o.CompletionTime); // ��������
                    foreach (var order in savedOrders)
                    {
                        WorkOrders.Add(order);
                    }
                }
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                DisplayAlert("����", "���ع�����¼ʧ��: " + ex.Message, "ȷ��");
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

                // ���б�ͷ�����¹���
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
                Console.WriteLine($"���湤��ʱ����: {ex.Message}");
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