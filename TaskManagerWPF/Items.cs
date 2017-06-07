namespace TaskManagerWPF
{
    /// <summary>
    /// класс для получение информации о процессе
    /// </summary>
    public class ProcessInfo
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public string State { get; set; }
        public string Username { get; set; }

        public long Memory { get; set; }

        public string Description { get; set; }
        public string Priority { get; set; }
    }
}