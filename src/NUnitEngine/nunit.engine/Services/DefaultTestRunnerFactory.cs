﻿// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// DefaultTestRunnerFactory handles creation of a suitable test
    /// runner for a given package to be loaded and run either in a
    /// separate process or within the same process.
    /// </summary>
    public class DefaultTestRunnerFactory : InProcessTestRunnerFactory, ITestRunnerFactory
    {
#if !NETSTANDARD1_6
        private IProjectService _projectService;
#endif

        #region Service Overrides

        public override void StartService()
        {
#if !NETSTANDARD1_6
            // TestRunnerFactory requires the ProjectService
            _projectService = ServiceContext.GetService<IProjectService>();

            // Anything returned from ServiceContext is known to be an IService
            Status = _projectService != null && ((IService)_projectService).Status == ServiceStatus.Started
                ? ServiceStatus.Started
                : ServiceStatus.Error;
#else
            Status = ServiceStatus.Started;
#endif
        }

        #endregion

        /// <summary>
        /// Returns a test runner based on the settings in a TestPackage.
        /// Any setting that is "consumed" by the factory is removed, so
        /// that downstream runners using the factory will not repeatedly
        /// create the same type of runner.
        /// </summary>
        /// <param name="package">The TestPackage to be loaded and run</param>
        /// <returns>A TestRunner</returns>
        public override ITestEngineRunner MakeTestRunner(TestPackage package)
        {
#if NETSTANDARD1_6 || NETSTANDARD2_0
            if (package.HasMultipleAssemblies())
                return new AggregatingTestRunner(this.ServiceContext, package);

            return base.MakeTestRunner(package);
        }
#else

            ProcessModel processModel = GetTargetProcessModel(package);

            switch (processModel)
            {
                default:
                case ProcessModel.Default:
                    if (package.HasMultipleAssemblies())
                        return new MultipleTestProcessRunner(this.ServiceContext, package);
                    else
                        return new ProcessRunner(this.ServiceContext, package);

                case ProcessModel.Multiple:
                    return new MultipleTestProcessRunner(this.ServiceContext, package);

                case ProcessModel.Separate:
                    return new ProcessRunner(this.ServiceContext, package);

                case ProcessModel.InProcess:
                    return base.MakeTestRunner(package);
            }
        }

        // TODO: Review this method once we have a gui - not used by console runner
        public override bool CanReuse(ITestEngineRunner runner, TestPackage package)
        {
            ProcessModel processModel = GetTargetProcessModel(package);

            switch (processModel)
            {
                case ProcessModel.Default:
                case ProcessModel.Multiple:
                    return runner is MultipleTestProcessRunner;
                case ProcessModel.Separate:
                    return runner is ProcessRunner;
                default:
                    return base.CanReuse(runner, package);
            }
        }

#region Helper Methods

        private ProcessModel GetTargetProcessModel(TestPackage package)
        {
            return (ProcessModel)System.Enum.Parse(
                typeof(ProcessModel),
                package.GetSetting(EnginePackageSettings.ProcessModel, "Default"));
        }

#endregion

#endif
    }
}
