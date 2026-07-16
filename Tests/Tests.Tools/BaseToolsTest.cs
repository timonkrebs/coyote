// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;

namespace Microsoft.Coyote.Tools.Tests
{
    public abstract class BaseToolsTest : BaseTest
    {
        public BaseToolsTest(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
