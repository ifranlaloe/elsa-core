using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

[Activity("Elsa", "Flow", "A flowchart is a collection of activities and connections between them.")]
public class Flowchart : Container
{
    internal const string ScopeProperty = "Scope";

    public Flowchart()
    {
        OnSignalReceived<ActivityCompleted>(OnDescendantCompletedAsync);
    }

    [Node] public IActivity? Start { get; set; }
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        if (Start == null!)
        {
            await context.CompleteActivityAsync();
            return;
        }

        context.ScheduleActivity(Start);
    }

    private async ValueTask OnDescendantCompletedAsync(ActivityCompleted signal, SignalContext context)
    {
        await ScheduleChildrenAsync(signal, context);
    }

    private async Task ScheduleChildrenAsync(ActivityCompleted signal, SignalContext context)
    {
        var flowchartActivityExecutionContext = context.ReceiverActivityExecutionContext;
        var completedActivity = context.SenderActivityExecutionContext.Activity;

        if (completedActivity == null!)
            return;

        // Ignore completed activities that are not immediate children.
        var isDirectChild = Activities.Contains(completedActivity);

        if (!isDirectChild)
            return;

        // If a specific outcome was provided by the completed activity, use it to find the connection to the next activity.
        Func<Connection, bool> outboundConnectionsQuery = signal.Result is Outcome outcome
            ? connection => connection.Source == completedActivity && connection.SourcePort == outcome.Name
            : connection => connection.Source == completedActivity;

        var outboundConnections = Connections.Where(outboundConnectionsQuery).ToList();
        var children = outboundConnections.Select(x => x.Target).ToList();
        var scope = flowchartActivityExecutionContext.GetProperty(ScopeProperty, () => new FlowScope());

        scope.RegisterActivityExecution(completedActivity);

        if (children.Any())
        {
            scope.AddActivities(children);

            // Schedule each child, but only if all of its left inbound activities have already executed.
            foreach (var activity in children)
            {
                var inboundActivities = Connections.LeftInboundActivities(activity).ToList();

                // If the completed activity is not part of the left inbound path, always allow its children to be scheduled.
                if (!inboundActivities.Contains(completedActivity))
                {
                    flowchartActivityExecutionContext.ScheduleActivity(activity);
                    continue;
                }

                // If the activity is anything but a join activity, only schedule it if all of its left-inbound activities have executed, effectively implementing a "wait all" join. 
                if (activity is not IJoinNode)
                {
                    var executionCount = scope.GetExecutionCount(activity);
                    var haveInboundActivitiesExecuted = inboundActivities.All(x => scope.GetExecutionCount(x) > executionCount);

                    if (haveInboundActivitiesExecuted)
                        flowchartActivityExecutionContext.ScheduleActivity(activity);
                }
                else
                {
                    flowchartActivityExecutionContext.ScheduleActivity(activity);
                }
            }
        }

        if (!children.Any())
        {
            // If there are no more pending activities in any of the scopes, mark this activity as completed.
            var hasPendingChildren = scope.HasPendingActivities();

            if (!hasPendingChildren)
                await flowchartActivityExecutionContext.CompleteActivityAsync();
        }

        context.StopPropagation();
    }
}