
export class MockHelper {

    public static mockServiceEndpoint(endpointId: string, authParams: any, dataParams: any = null): void {
        Object.keys(authParams).forEach(key =>
            process.env[`ENDPOINT_AUTH_PARAMETER_${endpointId}_${key.toUpperCase()}`] = authParams[key]);

        if (dataParams) {
            Object.keys(dataParams).forEach(key =>
                process.env[`ENDPOINT_DATA_${endpointId}_${key.toUpperCase()}`] = dataParams[key]);
        }
    }
}
