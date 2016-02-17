using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;

using System.Activities;
using Orleans.Activities;
using Orleans.Activities.Sample.HelloWorld.GrainInterfaces;

namespace Orleans.Activities.Sample.HelloWorld.Grains
{
    public class HelloGrainState : WorkflowState
    { }

    public interface IHelloWorkflowInterface
    {
        // These are the operations that the grain calls on the workflow, these shouldn't be the same as the IHello grain interface method!
        // There are 2 restrictions on the methods:
        // - it must have 1 parameter, with type Func<Task<anything>> or Func<Task> (executed when the workflow accepts the request)
        // - the return type can be Task or Task<anything>
        Task<string> GreetClient(Func<Task<string>> clientSaid);
        Task<string> FarewellClient(Func<Task> request);
    }

    public interface IHelloWorkflowCallbackInterface
    {
        // This is the operation that the workflow calls back on the grain.
        // There are 2 restrictions on the methods:
        // - it can have max. 1 parameter with any type
        // - the return type can be Task<Func<Task<anything>>> or Task<Func<Task>> (executed when the workflow accepts the response)
        Task<Func<Task<string>>> WhatShouldISay(string clientSaid);
    }

    public sealed class HelloGrain : WorkflowGrain<HelloGrain, HelloGrainState, IHelloWorkflowInterface, IHelloWorkflowCallbackInterface>, IHello, IHelloWorkflowCallbackInterface
    {
        // Without DI and versioning, just directly create the workflow definition.
        public HelloGrain()
            : base((wi) => new HelloActivity(), null)
        {
            WorkflowControl.ExtensionsFactory = () => new GrainTrackingParticipant(GetLogger()).Yield();
        }

        // Mandatory: at least log the unhandled exceptions (workflow will abort by default, see Parameters property).
        protected override Task OnUnhandledExceptionAsync(Exception exception, Activity source)
        {
            GetLogger().Error(0, $"OnUnhandledExceptionAsync: the workflow is going to {Parameters.UnhandledExceptionAction}", exception);
            return Task.CompletedTask;
        }

        // The parameter delegate executed when the workflow accepts the incoming call,
        // it can modify the grain's State or do nearly anything a normal grain method can (command pattern).
        public async Task<string> SayHello(string greeting)
        {
            try
            {
                return await WorkflowInterface.GreetClient(() =>
                    Task.FromResult(greeting));
            }
            catch (OperationRepeatedException<string> e)
            {
                return e.PreviousResponseParameter;
            }
        }

        // The parameter delegate executed when the workflow accepts the incoming call,
        // it can modify the grain's State or do nearly anything a normal grain method can (command pattern).
        public async Task<string> SayBye()
        {
            try
            {
                return await WorkflowInterface.FarewellClient(() =>
                    Task.CompletedTask);
            }
            catch (OperationRepeatedException<string> e)
            {
                return e.PreviousResponseParameter;
            }
            catch (OperationCanceledException)
            {
                return "Sorry, we have waited for your farewell, but gave up!";
            }
        }

        // The return value delegate executed when the workflow accepts the outgoing call's response,
        // it can modify the grain's State or do nearly anything a normal grain method can (command pattern).
        Task<Func<Task<string>>> IHelloWorkflowCallbackInterface.WhatShouldISay(string clientSaid) =>
            Task.FromResult<Func<Task<string>>>(() =>
                Task.FromResult(string.IsNullOrEmpty(clientSaid) ? "Who are you?" : "Hello!"));
    }
}
