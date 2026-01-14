"""Shared pytest fixtures for e2e tests."""

import pytest_asyncio

from .testharness import E2ETestContext


@pytest_asyncio.fixture(scope="module", loop_scope="module")
async def ctx():
    """Create and teardown a test context shared across all tests in this module."""
    context = E2ETestContext()
    await context.setup()
    yield context
    await context.teardown()


@pytest_asyncio.fixture(autouse=True, loop_scope="module")
async def configure_test(request, ctx):
    """Automatically configure the proxy for each test."""
    # Extract test file name from module (e.g., "test_session" -> "session")
    module_name = request.module.__name__.split(".")[-1]
    if module_name.startswith("test_"):
        test_file = module_name[5:]  # Remove "test_" prefix
    else:
        test_file = module_name

    # Extract test name (e.g., "test_should_create_sessions" -> "should_create_sessions")
    test_name = request.node.name
    if test_name.startswith("test_"):
        test_name = test_name[5:]  # Remove "test_" prefix

    await ctx.configure_for_test(test_file, test_name)
    yield
