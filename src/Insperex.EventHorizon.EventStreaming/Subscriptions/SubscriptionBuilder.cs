using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Interfaces.Internal;
using Insperex.EventHorizon.Abstractions.Models;
using Insperex.EventHorizon.Abstractions.Reflection;
using Insperex.EventHorizon.EventStreaming.Interfaces.Streaming;
using Insperex.EventHorizon.EventStreaming.Subscriptions.Backoff;
using Microsoft.Extensions.Logging;

namespace Insperex.EventHorizon.EventStreaming.Subscriptions;

public class SubscriptionBuilder<TMessage> where TMessage : class, ITopicMessage, new()
{
    private readonly IStreamFactory _factory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITopicAdmin<TMessage> _admin;
    private readonly List<string> _topics;
    private readonly Dictionary<string, Type> _typeDict = new();
    private int? _batchSize = 1000;
    private bool? _isBeginning = true;
    private TimeSpan _noBatchDelay = TimeSpan.FromMilliseconds(10);
    private DateTime? _startDateTime;
    private string _subscriptionName = AssemblyUtil.AssemblyName;
    private bool _redeliverFailedMessages = true;
    private bool _guaranteeMessageOrderOnFailure;
    private IBackoffStrategy _backoffStrategy = new ConstantBackoffStrategy {Delay = TimeSpan.FromMilliseconds(10)};
    private Func<SubscriptionContext<TMessage>, Task> _onBatch;
    private SubscriptionType _subscriptionType = Abstractions.Models.SubscriptionType.KeyShared;
    private bool _isPreload;

    public SubscriptionBuilder(IStreamFactory factory, ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _loggerFactory = loggerFactory;
        _topics = new List<string>();
        _admin = _factory.CreateAdmin<TMessage>();
    }

    public SubscriptionBuilder<TMessage> AddStateStream<TState>(string senderId = null) where TState : IState
    {
        var stateType = typeof(TState);
        var stateDetails = ReflectionFactory.GetStateDetail(stateType);

        // Add types
        foreach (var type in stateDetails.ActionDict)
            _typeDict[type.Key] = type.Value;

        // Add Sub Topics (for IState only)
        var topics = stateDetails.AllStateTypes
            .Select(x => _admin.GetTopic(x, senderId))
            .Where(x => x != null)
            .ToArray();

        _topics.AddRange(topics);

        return this;
    }
    public SubscriptionBuilder<TMessage> AddStream<TAction>(string senderId = null) where TAction : IAction
    {
        var actionType = typeof(TAction);

        // Add types
        var types = ReflectionFactory.GetTypeDetail(actionType).GetTypes<TAction>();
        foreach (var type in types)
            _typeDict[type.Name] = type;

        // Add Main Topic
        _topics.Add(_admin.GetTopic(actionType, senderId));

        return this;
    }

    public SubscriptionBuilder<TMessage> SubscriptionName(string name)
    {
        _subscriptionName = $"{AssemblyUtil.AssemblyName}-{name}";
        return this;
    }

    public SubscriptionBuilder<TMessage> SubscriptionType(SubscriptionType subscriptionType)
    {
        _subscriptionType = subscriptionType;
        return this;
    }

    public SubscriptionBuilder<TMessage> NoBatchDelay(TimeSpan delay)
    {
        _noBatchDelay = delay;
        return this;
    }

    public SubscriptionBuilder<TMessage> BatchSize(int size)
    {
        _batchSize = size;
        return this;
    }

    public SubscriptionBuilder<TMessage> StartDateTime(DateTime startDateTime)
    {
        _startDateTime = startDateTime;
        return this;
    }

    public SubscriptionBuilder<TMessage> IsBeginning(bool isBeginning)
    {
        _isBeginning = isBeginning;
        return this;
    }

    public SubscriptionBuilder<TMessage> IsPreLoad(bool isPreload)
    {
        _isPreload = isPreload;
        return this;
    }

    public SubscriptionBuilder<TMessage> RedeliverFailedMessages(bool redeliver)
    {
        _redeliverFailedMessages = redeliver;
        return this;
    }

    public SubscriptionBuilder<TMessage> GuaranteeMessageOrderOnFailure(bool guarantee)
    {
        _guaranteeMessageOrderOnFailure = guarantee;
        return this;
    }

    public SubscriptionBuilder<TMessage> BackoffStrategy(IBackoffStrategy backoffStrategy)
    {
        _backoffStrategy = backoffStrategy;
        return this;
    }

    public SubscriptionBuilder<TMessage> OnBatch(Func<SubscriptionContext<TMessage>, Task> onBatch)
    {
        _onBatch = onBatch;
        return this;
    }

    public Subscription<TMessage> Build()
    {
        EnsureValid();

        var config = new SubscriptionConfig<TMessage>
        {
            Topics = _topics.Distinct().ToArray(),
            TypeDict = _typeDict,
            SubscriptionName = _subscriptionName,
            SubscriptionType = _subscriptionType,
            NoBatchDelay = _noBatchDelay,
            BatchSize = _batchSize,
            StartDateTime = _startDateTime,
            IsBeginning = _isBeginning,
            IsPreload = _isPreload,
            RedeliverFailedMessages = _redeliverFailedMessages,
            IsMessageOrderGuaranteedOnFailure = _guaranteeMessageOrderOnFailure,
            BackoffStrategy = _backoffStrategy,
            OnBatch = _onBatch,
        };
        var logger = _loggerFactory.CreateLogger<Subscription<TMessage>>();

        // Return
        return new Subscription<TMessage>(_factory, config, logger);
    }

    private void EnsureValid()
    {
        var anyFailureHandling = _backoffStrategy != null || _guaranteeMessageOrderOnFailure;
        if (!_redeliverFailedMessages && anyFailureHandling)
        {
            throw new InvalidOperationException(
                "If any failure handling options are set, expect RedeliverFailedMessages to be true.");
        }
    }
}
