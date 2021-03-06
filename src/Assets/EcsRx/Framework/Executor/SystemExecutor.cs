﻿using System;
using System.Collections.Generic;
using System.Linq;
using EcsRx.Entities;
using EcsRx.Events;
using EcsRx.Extensions;
using EcsRx.Groups;
using EcsRx.Pools;
using EcsRx.Systems.Executor.Handlers;

namespace EcsRx.Systems.Executor
{
    public class SystemExecutor : ISystemExecutor
    {
        private readonly IList<ISystem> _systems; 
        private readonly Dictionary<ISystem, IList<SubscriptionToken>> _systemSubscriptions; 

        public IPoolManager PoolManager { get; private set; }
        public IEnumerable<ISystem> Systems { get { return _systems; } }

        public IReactToEntitySystemHandler ReactToEntitySystemHandler { get; private set; }
        public IReactToGroupSystemHandler ReactToGroupSystemHandler { get; private set; }
        public ISetupSystemHandler SetupSystemHandler { get; private set; }
        public IReactToDataSystemHandler ReactToDataSystemHandler { get; private set; }

        public SystemExecutor(IPoolManager poolManager, 
            IReactToEntitySystemHandler reactToEntitySystemHandler, IReactToGroupSystemHandler reactToGroupSystemHandler, 
            ISetupSystemHandler setupSystemHandler, IReactToDataSystemHandler reactToDataSystemHandler)
        {
            PoolManager = poolManager;
            ReactToEntitySystemHandler = reactToEntitySystemHandler;
            ReactToGroupSystemHandler = reactToGroupSystemHandler;
            SetupSystemHandler = setupSystemHandler;
            ReactToDataSystemHandler = reactToDataSystemHandler;

            PoolManager.OnEntityAdded += OnEntityAddedToPool;
            PoolManager.OnEntityRemoved += OnEntityRemovedFromPool;
            PoolManager.OnEntityComponentAdded += OnEntityComponentAdded;
            PoolManager.OnEntityComponentRemoved += OnEntityComponentRemoved;

            _systems = new List<ISystem>();
            _systemSubscriptions = new Dictionary<ISystem, IList<SubscriptionToken>>();
        }

        private void OnEntityComponentRemoved(object sender, EntityComponentEvent args)
        {
            var originalComponents = args.Entity.Components.ToList();
            originalComponents.Add(args.Component);

            var applicableSystems = _systems.GetApplicableSystems(originalComponents).ToArray();
            var effectedSystems = applicableSystems.Where(x => x.TargetGroup.TargettedComponents.Contains(args.Component.GetType()));
            effectedSystems.ForEachRun(system => _systemSubscriptions[system].Where(subscription => subscription.AssociatedObject == args.Entity));
        }

        private void OnEntityComponentAdded(object sender, EntityComponentEvent args)
        {
            var applicableSystems = _systems.GetApplicableSystems(args.Entity).ToArray();
            var effectedSystems = applicableSystems.Where(x => x.TargetGroup.TargettedComponents.Contains(args.Component.GetType()));

            ApplyEntityToSystems(effectedSystems, args.Entity);
        }

        public void OnEntityAddedToPool(object sender, PooledEntityEvent args)
        {
            var applicableSystems = _systems.GetApplicableSystems(args.Entity).ToArray();
            ApplyEntityToSystems(applicableSystems, args.Entity);
        }

        private void ApplyEntityToSystems(IEnumerable<ISystem> systems, IEntity entity)
        {
            systems.OfType<ISetupSystem>()
                .ForEachRun(x => x.Setup(entity));

            systems.OfType<IReactToEntitySystem>()
            .ForEachRun(x =>
            {
                var subscription = ReactToEntitySystemHandler.ProcessEntity(x, entity);
                _systemSubscriptions[x].Add(subscription);
            });
            
            systems.Where(x => x.IsReactiveDataSystem())
                .ForEachRun(x =>
                {
                    var subscription = ReactToDataSystemHandler.ProcessEntityWithoutType(x, entity);
                    _systemSubscriptions[x].Add(subscription);
                });
        }

        public void OnEntityRemovedFromPool(object sender, PooledEntityEvent args)
        {
            var applicableSystems = _systems.GetApplicableSystems(args.Entity).ToArray();
            applicableSystems.ForEachRun(x => RemoveSubscription(x, args.Entity));
        }

        public void RemoveSubscription(ISystem system, IEntity entity)
        {
            var subscriptionList = _systemSubscriptions[system];
            var subscriptionTokens = subscriptionList.GetTokensFor(entity).ToArray();

            if (!subscriptionTokens.Any()) { return; }

            subscriptionTokens.ForEachRun(x => subscriptionList.Remove(x));
            subscriptionTokens.DisposeAll();
        }

        public void RemoveSystem(ISystem system)
        {
            _systems.Remove(system);

            if (_systemSubscriptions.ContainsKey(system))
            {
                _systemSubscriptions[system].DisposeAll();
                _systemSubscriptions.Remove(system);
            }
        }

        public void AddSystem(ISystem system)
        {
            _systems.Add(system);

            if (system is ISetupSystem)
            {
                SetupSystemHandler.Setup(system as ISetupSystem);
            }

            if (system is IReactToGroupSystem)
            {
                var subscription = ReactToGroupSystemHandler.Setup(system as IReactToGroupSystem);
                _systemSubscriptions.Add(system, new List<SubscriptionToken> { subscription });
            }

            if (system is IReactToEntitySystem)
            {
                var subscriptions = ReactToEntitySystemHandler.Setup(system as IReactToEntitySystem);
                _systemSubscriptions.Add(system, new List<SubscriptionToken>(subscriptions));
            }
            
            if (system.IsReactiveDataSystem())
            {
                var subscriptions = ReactToDataSystemHandler.SetupWithoutType(system);
                _systemSubscriptions.Add(system, new List<SubscriptionToken>(subscriptions));
            }
        }
    }
}