﻿// Copyright 2011-2012 Anodyne.
//   
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
//  
//      http://www.apache.org/licenses/LICENSE-2.0 
//  
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

namespace Kostassoid.Anodyne.DataAccess.Specs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Anodyne.Specs.Shared;
    using Common.Tools;
    using Domain.Base;
    using Domain.Events;
    using Exceptions;
    using NUnit.Framework;
    using Policy;

    // ReSharper disable InconsistentNaming
    public class UnitOfWorkBasicSpecs
    {
        public class UnitOfWorkScenario
        {
            public UnitOfWorkScenario()
            {
                IntegrationContext.Init();
            }
        }

        [Serializable]
        public class TestRoot : AggregateRoot<Guid>
        {
            protected TestRoot()
            {
                Id = SeqGuid.NewGuid();
            }

            public static TestRoot Create()
            {
                var root = new TestRoot();
                Apply(new TestRootCreated(root));
                return root;
            }

            protected void OnCreated(TestRootCreated @event)
            {

            }

            public void Update()
            {
                Apply(new TestRootUpdated(this));
            }

            protected void OnUpdated(TestRootUpdated @event)
            {

            }
        }

        public class TestRootCreated : AggregateEvent<TestRoot>
        {
            public TestRootCreated(TestRoot aggregate)
                : base(aggregate)
            {
            }
        }

        public class TestRootUpdated : AggregateEvent<TestRoot>
        {
            public TestRootUpdated(TestRoot aggregate)
                : base(aggregate)
            {
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_working_with_root_from_one_unit_of_work : UnitOfWorkScenario
        {
            [Test]
            public void should_save_root_and_update_version()
            {
                Guid rootId;
                using (new UnitOfWork())
                {
                    rootId = TestRoot.Create().Id;
                }

                using (var uow = new UnitOfWork())
                {
                    var root = uow.Query<TestRoot>().FindOne(rootId);

                    Assert.That(root.IsSome, Is.True);
                    Assert.That(root.Value.Id, Is.EqualTo(rootId));
                    Assert.That(root.Value.Version, Is.EqualTo(1));
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_several_roots_within_one_unit_of_work : UnitOfWorkScenario
        {
            [Test]
            public void should_update_all_roots()
            {
                TestRoot root1;
                TestRoot root2;
                using (new UnitOfWork())
                {
                    root1 = TestRoot.Create();
                    root2 = TestRoot.Create();
                    root1.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    var foundRoot1 = uow.Query<TestRoot>().FindOne(root1.Id);
                    var foundRoot2 = uow.Query<TestRoot>().FindOne(root2.Id);
                    foundRoot2.Value.Update();
                    foundRoot2.Value.Update();
                    foundRoot2.Value.Update();
                    foundRoot1.Value.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    var foundRoot1 = uow.Query<TestRoot>().FindOne(root1.Id);
                    var foundRoot2 = uow.Query<TestRoot>().FindOne(root2.Id);

                    Assert.That(foundRoot1.Value.Id, Is.EqualTo(root1.Id));
                    Assert.That(foundRoot2.Value.Id, Is.EqualTo(root2.Id));
                    Assert.That(foundRoot1.Value.Version, Is.EqualTo(3));
                    Assert.That(foundRoot2.Value.Version, Is.EqualTo(4));
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_existing_root : UnitOfWorkScenario
        {
            [Test]
            public void should_update_root_version()
            {
                Guid rootId;
                using (new UnitOfWork())
                {
                    rootId = TestRoot.Create().Id;
                }

                using (var uow = new UnitOfWork())
                {
                    var root = uow.Query<TestRoot>().FindOne(rootId).Value;
                    root.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    var updatedRoot = uow.Query<TestRoot>().FindOne(rootId).Value;
                    Assert.That(updatedRoot.Version, Is.EqualTo(2));
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_root_several_times_in_one_uow : UnitOfWorkScenario
        {
            [Test]
            public void should_update_root_version()
            {
                TestRoot originalRoot;
                using (new UnitOfWork())
                {
                    originalRoot = TestRoot.Create();
                    originalRoot.Update();
                }

                Assert.That(originalRoot.Version, Is.EqualTo(2));

                using (var uow = new UnitOfWork())
                {
                    var root = uow.Query<TestRoot>().FindOne(originalRoot.Id).Value;
                    root.Update();
                    root.Update();
                    root.Update();
                    Assert.That(root.Version, Is.EqualTo(5));
                }

                using (var uow = new UnitOfWork())
                {
                    var root = uow.Query<TestRoot>().FindOne(originalRoot.Id).Value;
                    Assert.That(root.Version, Is.EqualTo(5));
                }

            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_root_and_there_is_a_newer_version : UnitOfWorkScenario
        {
            [Test]
            [ExpectedException(typeof(StaleDataException))]
            public void should_throw_stale_data_exception()
            {
                TestRoot root;
                using (new UnitOfWork())
                {
                    root = TestRoot.Create();
                    root.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    var anotherRoot = uow.Query<TestRoot>().FindOne(root.Id).Value;
                    anotherRoot.Update();
                }

                using (new UnitOfWork())
                {
                    root.Update();
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_root_and_there_is_a_newer_version_with_ignore_policy : UnitOfWorkScenario
        {
            [Test]
            public void should_overwrite_conflicting_roots()
            {
                TestRoot root;
                using (new UnitOfWork())
                {
                    root = TestRoot.Create();
                    root.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    var anotherRoot = uow.Query<TestRoot>().FindOne(root.Id).Value;
                    anotherRoot.Update();
                    anotherRoot.Update(); // should be version 4 at this point
                }

                using (new UnitOfWork(StaleDataPolicy.Ignore))
                {
                    root.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    root = uow.Query<TestRoot>().FindOne(root.Id).Value;
                    Assert.That(root.Version, Is.EqualTo(3));
                }

            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_root_and_there_is_a_newer_version_with_skip_policy : UnitOfWorkScenario
        {
            [Test]
            public void should_skip_conflicting_roots()
            {
                TestRoot root;
                using (new UnitOfWork())
                {
                    root = TestRoot.Create();
                    root.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    var anotherRoot = uow.Query<TestRoot>().FindOne(root.Id).Value;
                    anotherRoot.Update();
                    anotherRoot.Update(); // should be version 4 at this point
                }

                using (new UnitOfWork(StaleDataPolicy.SilentlySkip))
                {
                    root.Update();
                }

                using (var uow = new UnitOfWork())
                {
                    root = uow.Query<TestRoot>().FindOne(root.Id).Value;
                    Assert.That(root.Version, Is.EqualTo(4));
                }

            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_working_with_different_objects_of_the_same_root_from_single_unit_of_work : UnitOfWorkScenario
        {
            [Test]
            public void should_throw_concurrency_exception()
            {
                Guid rootId;
                using (new UnitOfWork())
                {
                    rootId = TestRoot.Create().Id;
                }

                using (var uow = new UnitOfWork())
                {
                    var root = uow.Query<TestRoot>().FindOne(rootId).Value;
                    root.Update();
                    var anotherRoot = uow.Query<TestRoot>().FindOne(rootId).Value;
                    Assert.Throws<ConcurrencyException>(anotherRoot.Update);
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_updating_root_from_nested_unit_of_work : UnitOfWorkScenario
        {
            [Test]
            public void should_update_root_version_and_correctly_dispose_unit_of_work()
            {
                Guid rootId;
                using (new UnitOfWork())
                {
                    rootId = TestRoot.Create().Id;
                }

                using (new UnitOfWork())
                {
                    using (var nestedUow = new UnitOfWork())
                    {
                        var root = nestedUow.Query<TestRoot>().FindOne(rootId).Value;
                        root.Update();
                    }
                }

                using (new UnitOfWork())
                {
                    using (var nestedUow = new UnitOfWork())
                    {
                        var root = nestedUow.Query<TestRoot>().FindOne(rootId).Value;
                        root.Update();
                    }
                }

                using (var uow = new UnitOfWork())
                {
                    var updatedRoot = uow.Query<TestRoot>().FindOne(rootId).Value;
                    Assert.That(updatedRoot.Version, Is.EqualTo(3));
                }

                Assert.That(UnitOfWork.Current.IsNone, Is.True);
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_trying_to_save_many_roots_at_the_same_time : UnitOfWorkScenario
        {
            [Explicit("No need to run every time perhaps.")]
            [Test]
            [MaxTime(1000)]
            public void should_save_all_roots_without_fail()
            {
                const int testingThreadsCount = 100;

                var tasks = new List<Task>(testingThreadsCount);

                var threadsToStart = testingThreadsCount;
                while (threadsToStart-- > 0)
                {
                    var task = new Task(() =>
                    {
                        using (new UnitOfWork())
                        {
                            TestRoot.Create();
                        }
                    });

                    tasks.Add(task);
                }

                tasks.ForEach(t => t.Start());
                Task.WaitAll(tasks.ToArray());

                using (var unitOfWork = new UnitOfWork())
                {
                    Assert.AreEqual(testingThreadsCount, unitOfWork.Query<TestRoot>().Count());
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_trying_to_update_root_from_one_thread_with_ignore_stale_policy : UnitOfWorkScenario
        {
            [Explicit("No need to run every time perhaps.")]
            [Test]
            [MaxTime(1000)]
            public void should_not_fail()
            {
                const int testingCount = 100;

                Guid id;

                using (new UnitOfWork())
                {
                    id = TestRoot.Create().Id;
                }

                var toStart = testingCount;
                while (toStart-- > 0)
                {
                    using (var unitOfWork = new UnitOfWork(StaleDataPolicy.Ignore))
                    {
                        var root = unitOfWork.Query<TestRoot>().GetOne(id);
                        root.Update();
                    }
                }

                using (var unitOfWork = new UnitOfWork())
                {
                    var root = unitOfWork.Query<TestRoot>().GetOne(id);
                    Assert.That(root.Version, Is.EqualTo(testingCount + 1));
                }
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class when_trying_to_update_root_from_many_threads_with_ignore_stale_policy : UnitOfWorkScenario
        {
            [Explicit("No need to run every time perhaps.")]
            [Test]
            [MaxTime(1000)]
            public void should_not_fail()
            {
                const int testingThreadsCount = 100;

                var tasks = new List<Task>(testingThreadsCount);

                Guid id;

                using (new UnitOfWork())
                {
                    id = TestRoot.Create().Id;
                }

                var threadsToStart = testingThreadsCount;
                while (threadsToStart-- > 0)
                {
                    var task = new Task(() =>
                    {
                        using (var unitOfWork = new UnitOfWork(StaleDataPolicy.Ignore))
                        {
                            var root = unitOfWork.Query<TestRoot>().GetOne(id);
                            root.Update();
                        }
                    });

                    tasks.Add(task);
                }
                tasks.ForEach(t => t.Start());
                Task.WaitAll(tasks.ToArray());

                using (var unitOfWork = new UnitOfWork())
                {
                    var root = unitOfWork.Query<TestRoot>().GetOne(id);
                    Assert.That(root.Version, Is.LessThanOrEqualTo(testingThreadsCount + 1));
                }
            }
        }
    }
    // ReSharper restore InconsistentNaming

}
