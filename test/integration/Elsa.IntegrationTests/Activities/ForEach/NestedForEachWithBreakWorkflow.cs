using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.IntegrationTests.Activities;

class NestedForEachWithBreakWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var outerItems = new[] { "C#", "Rust", "Go" };
        var innerItems = new[] { "Classes", "Functions", "Modules" };
        var currentOuterItem = new Variable<string>();
        var currentInnerItem = new Variable<string>();

        workflow.Root = new ForEach<string>(outerItems)
        {
            CurrentValue = new Output<string?>(currentOuterItem),
            Body = new Sequence
            {
                Activities =
                {
                    new WriteLine(currentOuterItem),
                    new ForEach<string>(innerItems)
                    {
                        CurrentValue = new Output<string?>(currentInnerItem),
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new If(context => currentInnerItem.Get(context) == "Functions")
                                {
                                    Then = new Break()
                                },
                                new WriteLine(currentInnerItem)
                            }
                        }
                    }
                }
            }
        };
    }
}