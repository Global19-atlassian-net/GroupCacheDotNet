﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GroupCache
{
    public class SimpleRetryPolicy
    {
        public const int DEFAULT_MAX_ATTEMPTS = 3;
        public static readonly TimeSpan DEFAULT_BACK_OFF_PERIOD = TimeSpan.FromMilliseconds(1000);
        private volatile int _maxAttempts;
        private volatile int _backOffPeriod;
        private Type[] _retryableExceptionTypes;

        public SimpleRetryPolicy() :
            this(DEFAULT_MAX_ATTEMPTS, DEFAULT_BACK_OFF_PERIOD)
        {
        }

        public SimpleRetryPolicy(int maxAttempts, TimeSpan backOffPeriod) :
            this(maxAttempts, backOffPeriod, new Type[] { typeof(Exception) })
        {
        }

        public SimpleRetryPolicy(int maxAttempts, TimeSpan backOffPeriod, Type[] retryableExceptions)
        {
            _maxAttempts = maxAttempts;
            _backOffPeriod = (int)backOffPeriod.TotalMilliseconds;
            _retryableExceptionTypes = retryableExceptions;
        }

        private bool RetryForException(Exception classifiable)
        {
            if (classifiable == null)
            {
                return false;
            }

            var exceptionClass = classifiable.GetType();

            if (_retryableExceptionTypes == null)
            {
                return false;
            }
            else
            {
                // Determines whether the exception is a subclass of any element of the retryable exception type.
                return _retryableExceptionTypes.Any((retryableType) => { return retryableType.IsAssignableFrom(exceptionClass); });
            }
        }

        /// <summary>
        /// Gets the maximum retry count.
        /// </summary>
        /// <value>The maximum retry count.</value>
        public int MaxAttempts
        {
            get { return _maxAttempts; }
            set { _maxAttempts = value; }
        }

        /// <summary>
        /// Gets or sets the delay time span to sleep after an exception is thrown and a rety is
        /// attempted.
        /// </summary>
        /// <value>The delay time span.</value>
        public TimeSpan BackOffPeriod
        {
            get { return TimeSpan.FromMilliseconds(_backOffPeriod); }
            set { _backOffPeriod = (int)value.TotalMilliseconds; }
        }

        public virtual bool CanRetry(RetryContext context)
        {
            Exception ex = context.LastException;
            // N.B. the contract is defined to include the initial attempt in the count.
            return (ex == null || RetryForException(ex)) && (context.RetryCount < _maxAttempts);
        }

        public virtual void RegisterThrowable(RetryContext context, Exception exception)
        {
            context.RegisterException(exception);
        }

        public virtual void BackOff(RetryContext context)
        {
            Thread.Sleep(BackOffPeriod);
        }

        public virtual Task BackOffAsync(RetryContext context)
        {
            return Task.Delay(BackOffPeriod);
        }

        public virtual T HandleRetryExhausted<T>(RetryContext context)
        {
            throw new ExhaustedRetryException("Retry exhausted after last attempt with no recovery path.", context.LastException);
        }
    }
}