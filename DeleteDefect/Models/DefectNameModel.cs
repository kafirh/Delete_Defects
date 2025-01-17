namespace DeleteDefect.Models
{
    public class DefectNameModel
    {
        public int Id { get; set; } // Primary key
        public string DefectName { get; set; }
        public int Priority { get; set; }
        public int ChartId { get; set; }
        public CharModel? Char { get; set; }
    }
}
