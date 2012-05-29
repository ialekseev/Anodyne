// Copyright 2011-2012 Anodyne.
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

namespace Kostassoid.Anodyne.DataAccess
{
    using Common;
    using Common.CodeContracts;
    using Common.ExecutionContext;
    using Common.Reflection;
    using Domain.Events;
    using Events;
    using Domain.Base;
    using Exceptions;

    using Kostassoid.Anodyne.Domain;

    using Operations;
    using Policy;
    using Wiring;
    using System;
    using System.Linq;

    public class UnitOfWork : IDisposable
    {
        private static IDataSessionFactory _dataSessionFactory;
        private static DataAccessPolicy _policy = new DataAccessPolicy();

        private const string RootContextKey = "unit-of-work";
        private const string HeadContextKey = "head-unit-of-work";

        private readonly string _contextKey = RootContextKey;
        private readonly UnitOfWork _parent;

        public IDataSession DataSession { get; protected set; }

        private StaleDataPolicy _staleDataPolicy = StaleDataPolicy.Strict;

        public static Option<UnitOfWork> Current
        {
            get { return Context.FindAs<UnitOfWork>(HeadContextKey); }
            protected set { Context.Set(HeadContextKey, value.IsSome ? value.Value : null); }
        }

        public bool IsRoot
        {
            get { return _parent == null; }
        }

        public bool IsFinished { get; protected set; }

        public bool IsDisposed
        {
            get { return Context.Find(_contextKey).IsNone; }
        }

        public static void SetFactory(IDataSessionFactory dataSessionFactory)
        {
            _dataSessionFactory = dataSessionFactory;
        }

        public static void EnforcePolicy(DataAccessPolicy policy)
        {
            _policy = policy;
        }

        static UnitOfWork()
        {
            EventBus
                .SubscribeTo()
                .AllBasedOn<IAggregateEvent>(From.Assemblies(_ => true))
                .With(e =>
                          {
                              if (_policy.ReadOnly)
                                  throw new InvalidOperationException("You can't mutate AggregateRoots in ReadOnly mode.");

                              if (Current.IsSome && !Current.Value.IsFinished)
                                  ((UnitOfWork)Current).DataSession.Handle(e);
                          });
        }

        public UnitOfWork(StaleDataPolicy? staleDataPolicy = null)
        {
            var parentUnitOfWork = Context.FindAs<UnitOfWork>(_contextKey);
            if (parentUnitOfWork.IsSome)
            {
                _parent = parentUnitOfWork.Value;
                DataSession = _parent.DataSession;

                _contextKey = String.Format("{0}-{1}", _contextKey, Guid.NewGuid().ToString("N"));
            }
            else
            {
                DataSession = _dataSessionFactory.OpenSession();
                if (DataSession == null)
                    throw new Exception("Unable to create IDataSession (bad configuration?)");
            }

            _staleDataPolicy = staleDataPolicy.HasValue
                ? staleDataPolicy.Value
                : _policy.StaleDataPolicy;

            Context.Set(_contextKey, this);
            Current = this;

            IsFinished = false;
        }

        protected void AssertIfFinished()
        {
            Assumes.True(!IsFinished, "This UnitOfWork is finished");
        }

        public void Complete()
        {
            AssertIfFinished();

            IsFinished = true;

            if (!IsRoot) return;

            EventBus.Publish(new UnitOfWorkCompletingEvent(this));
            var changeSet = DataSession.SaveChanges(_staleDataPolicy);
            EventBus.Publish(new UnitOfWorkCompletedEvent(this, changeSet));

            if (changeSet.StaleDataDetected && _staleDataPolicy == StaleDataPolicy.Strict)
                throw new StaleDataException(changeSet.StaleData, "Some aggregates weren't saved due to stale data (version mismatch)");
        }

        public void Rollback()
        {
            AssertIfFinished();

            IsFinished = true;

            if (!IsRoot) return;

            DataSession.Rollback();
            EventBus.Publish(new UnitOfWorkRollbackEvent(this));
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            try
            {
                if (!IsFinished)
                    Complete();
            }
            finally
            {
                Context.Release(_contextKey);
                Current = _parent;

                if (IsRoot)
                {
                    EventBus.Publish(new UnitOfWorkDisposingEvent(this));
                    DataSession.Dispose();
                }
            }
        }

        ~UnitOfWork()
        {
            if (!IsDisposed)
                throw new InvalidOperationException("UnitOfWork must be properly disposed!");
        }

        public IRepository<TRoot> Query<TRoot>() where TRoot : class, IAggregateRoot
        {
            AssertIfFinished();

            return DataSession.GetRepository<TRoot>();
        }

        public IQueryable<TRoot> AllOf<TRoot>() where TRoot : class, IAggregateRoot
        {
            AssertIfFinished();

            return Query<TRoot>().All();
        }

        public TOp Using<TOp>() where TOp : class, IDomainOperation
        {
            AssertIfFinished();

            return DataSession.GetOperation<TOp>();
        }

        public void MarkAsDeleted<TRoot>(TRoot entity) where TRoot : class, IAggregateRoot
        {
            AssertIfFinished();

            DataSession.MarkAsDeleted(entity);
        }

    }
}