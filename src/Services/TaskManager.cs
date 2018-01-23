namespace BrockBot.Services
{
    using Microsoft.Win32.TaskScheduler;

    public static class TaskManager
    {
        public static bool StartTask(string name)
        {
            foreach (var task in TaskService.Instance.RootFolder.EnumerateTasks())
            {
                if (task.Name.Contains(name))
                {
                    task.Run();
                    return task.State == TaskState.Running;
                }
            }

            return false;
        }

        public static bool StopTask(string name)
        {
            foreach (var task in TaskService.Instance.RootFolder.EnumerateTasks())
            {
                if (task.Name.Contains(name))
                {
                    task.Stop();
                    return task.State == TaskState.Ready;
                }
            }

            return false;
        }
    }
}