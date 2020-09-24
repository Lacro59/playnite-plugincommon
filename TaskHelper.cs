using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PluginCommon
{
    public class SystemCheckerTask
    {
        public CancellationTokenSource tokenSource { get; set; }
        public Task task { get; set; }
    }


    class TaskHelper
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        private List<SystemCheckerTask> SystemCheckerTask = new List<SystemCheckerTask>();


        public void Add(Task task, CancellationTokenSource tokenSource)
        {
            SystemCheckerTask.Add(new SystemCheckerTask { task = task, tokenSource = tokenSource });
        }

        public void Check()
        {
            try
            {
                List<SystemCheckerTask> TaskDelete = new List<SystemCheckerTask>();

#if DEBUG
                logger.Debug($"PluginCommon - SystemCheckerTask {SystemCheckerTask.Count}");
#endif

                // Check task status
                foreach (var taskRunning in SystemCheckerTask)
                {
                    if (taskRunning.task.Status != TaskStatus.RanToCompletion)
                    {
#if DEBUG
                        logger.Debug($"SystemChecker - Task {taskRunning.task.Id} ({taskRunning.task.Status}) is canceled");
#endif
                        // Cancel task if not terminated
                        taskRunning.tokenSource.Cancel();
                    }
                    else
                    {
                        // Add for delete
                        TaskDelete.Add(taskRunning);
                    }
                }

                // Delete tasks
                foreach (var taskRunning in TaskDelete)
                {
                    SystemCheckerTask.Remove(taskRunning);
#if DEBUG
                    logger.Debug($"SystemChecker - Task {taskRunning.task.Id} ({taskRunning.task.Status}) is removed");
#endif
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", "Error on Check()");
            }
        }
    }
}
