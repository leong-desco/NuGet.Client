// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Moq;
using NuGet.Common;
using NuGet.Packaging.Signing;
using Xunit;

namespace NuGet.Packaging.Test
{
    public class X509ChainBuildPolicyFactoryTests
    {
        [Fact]
        public void Create_WhenArgumentIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => X509ChainBuildPolicyFactory.Create(reader: null));

            Assert.Equal("reader", exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(",")]
        [InlineData("-1,2")]
        [InlineData("1,-2")]
        [InlineData("1,2,3")]
        public void Create_WhenEnvironmentVariableIsInvalid_ReturnsDefaultPolicy(string value)
        {
            Mock<IEnvironmentVariableReader> reader = CreateMockEnvironmentVariableReader(value);

            IX509ChainBuildPolicy policy = X509ChainBuildPolicyFactory.Create(reader.Object);

            Assert.IsType<DefaultX509ChainBuildPolicy>(policy);

            reader.VerifyAll();
        }

        [Theory]
        [InlineData("0,0")]
        [InlineData("1,0")]
        [InlineData("3,7")]
        public void Create_WhenEnvironmentVariableIsValid_ReturnsRetriablePolicy(string value)
        {
            Mock<IEnvironmentVariableReader> reader = CreateMockEnvironmentVariableReader(value);

            IX509ChainBuildPolicy policy = X509ChainBuildPolicyFactory.Create(reader.Object);

            Assert.IsType<RetriableX509ChainBuildPolicy>(policy);

            var retryPolicy = (RetriableX509ChainBuildPolicy)policy;

            string[] parts = value.Split(X509ChainBuildPolicyFactory.ValueDelimiter);
            int expectedRetryCount = int.Parse(parts[0]);
            TimeSpan expectedSleepInterval = TimeSpan.FromMilliseconds(int.Parse(parts[1]));

            Assert.Equal(expectedRetryCount, retryPolicy.RetryCount);
            Assert.Equal(expectedSleepInterval, retryPolicy.SleepInterval);

            reader.VerifyAll();
        }

        private static Mock<IEnvironmentVariableReader> CreateMockEnvironmentVariableReader(
            string variableValue)
        {
            var reader = new Mock<IEnvironmentVariableReader>(MockBehavior.Strict);

            reader.Setup(r => r.GetEnvironmentVariable(X509ChainBuildPolicyFactory.EnvironmentVariableName))
                .Returns(variableValue);

            return reader;
        }
    }
}
