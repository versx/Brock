namespace BrockBot.Services
{
    using System;

    using Microsoft.Win32.TaskScheduler;

    public static class TaskManager
    {
        public static bool StartTask(string name)
        {
            foreach (var task in TaskService.Instance.RootFolder.EnumerateTasks())
            {
                Console.WriteLine($"Task: {task.Name}");
                if (task.Name.ToLower().Contains(name.ToLower()))
                {
                    Console.WriteLine($"Task {name} found, starting...");
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
                Console.WriteLine($"Task: {task.Name}");
                if (task.Name.ToLower().Contains(name.ToLower()))
                {
                    Console.WriteLine($"Task {name} found, stopping...");
                    task.Stop();
                    return task.State == TaskState.Ready;
                }
            }

            return false;
        }

        public static bool RestartTask(string name)
        {
            foreach (var task in TaskService.Instance.RootFolder.EnumerateTasks())
            {
                Console.WriteLine($"Task: {task.Name}");
                if (task.Name.ToLower().Contains(name.ToLower()))
                {
                    if (task.State == TaskState.Running)
                    {
                        task.Stop();
                    }

                    if (task.State == TaskState.Ready)
                    {
                        task.Run();
                    }

                    return task.State == TaskState.Running;
                }
            }

            return false;
        }
    }
}