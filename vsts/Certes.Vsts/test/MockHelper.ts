
export interface IEndpointInfo {
    endpointId: string,
    authParams?: { [key: string]: string; },
    dataParams?: { [key: string]: string; },
}

export class MockHelper {

    public static mockServiceEndpoint(endpointInfo: IEndpointInfo): void {
        const endpointId = endpointInfo.endpointId;
        const authParams = endpointInfo.authParams;
        const dataParams = endpointInfo.dataParams;

        if (authParams) {
            Object.keys(authParams).forEach(key =>
                process.env[`ENDPOINT_AUTH_PARAMETER_${endpointId}_${key.toUpperCase()}`] = authParams[key]);
        }

        if (dataParams) {
            Object.keys(dataParams).forEach(key =>
                process.env[`ENDPOINT_DATA_${endpointId}_${key.toUpperCase()}`] = dataParams[key]);
        }
    }
}
