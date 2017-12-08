
export class MockHelper {
    
        public static mockServiceEndpoint(endpointId: string, params: any): void {
            Object.keys(params).forEach(key => {
                process.env[`ENDPOINT_AUTH_PARAMETER_${endpointId}_${key}`] = params[key];
            });
        }
    }
    