/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

import sdkProtocolVersion from "../../sdk-protocol-version.json";

/**
 * Gets the SDK protocol version from sdk-protocol-version.json.
 * @returns The protocol version number
 */
export function getSdkProtocolVersion(): number {
    return sdkProtocolVersion.version;
}
