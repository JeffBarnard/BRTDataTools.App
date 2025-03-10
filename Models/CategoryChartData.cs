namespace BRTDataTools.App.Models
{
    public class CategoryChartData
    {
        public string XValue { get; set; } = string.Empty;
        public int YValue { get; set; }

        public CategoryChartData(string title, int count)
        {
            XValue = title;
            YValue = count;
        }
    }
}