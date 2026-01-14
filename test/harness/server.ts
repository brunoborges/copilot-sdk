/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

import { ReplayingCapiProxy } from "./replayingCapiProxy";

// Starts up an instance of the ReplayingCapiProxy server
// The intention is for this to be usable in E2E tests across all languages

const proxy = new ReplayingCapiProxy("https://api.githubcopilot.com");
const proxyUrl = await proxy.start();

console.log(`Listening: ${proxyUrl}`);
