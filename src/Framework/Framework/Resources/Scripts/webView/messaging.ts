import { initCompleted, spaNavigated, spaNavigating } from "../events";

type WebViewMessageEnvelope = {
    type: string;
    messageId?: number;
    payload?: any;
};
type HttpRequestInputMessage = {
    url: string;
    method: string;
    headers: { key: string, value: string }[];
    bodyString: string;
};
type HttpRequestOutputMessage = {
    statusCode: number;
    headers: { key: string, value: string }[];
    bodyString: string;
};
type ResultMessage = {
    succeeded: boolean;
    content?: string;
};
type NavigationCompletedMessage = {
    routeName: string;
    routeParameters: { key: string, value: string }[];
};
type PageNotificationMessage = {
    methodName: string;
    args: any[];
};

const pendingRequests: { resolve: (result: any) => void, reject: (result: any) => void }[] = [];

// send messages
export function sendMessage(message: WebViewMessageEnvelope) {
    (window.external as any).sendMessage(JSON.stringify(message));
}

export async function sendMessageAndWaitForResponse<T>(messageType: string, message: any): Promise<T> {
    const envelope: WebViewMessageEnvelope = {
        type: messageType,
        messageId: pendingRequests.length,
        payload: message
    };

    const promise = new Promise<T>((resolve, reject) => {
        pendingRequests[envelope.messageId!] = { resolve, reject };
        sendMessage(envelope);
    });
    return await promise;
}

function processMessage(envelope: WebViewMessageEnvelope) {
    if (envelope.type == "GetViewModelSnapshot") {
        return <ResultMessage>{
            content: JSON.stringify(dotvvm.state)
        };
    } else if (envelope.type == "PatchViewModel") {
        const resultMessage: ResultMessage = {
            succeeded: true
        };
        try {
            dotvvm.patchState(envelope.payload);
        }
        catch (err) {
            resultMessage.succeeded = false;
            resultMessage.content = JSON.stringify(err);
        }
        return resultMessage;
    }
    else {
        throw `Command ${envelope.type} not found!`;
    }
}

export function initWebViewMessaging() {
    // handle commands from the webview
    (window.external as any).receiveMessage(async (json: any) => {

        function processRequestOrResponse(envelope: WebViewMessageEnvelope) {

            if (envelope.type === "HttpRequest") {
                // handle incoming HTTP request responses
                const promise = pendingRequests[envelope.messageId!]
                const message = <HttpRequestOutputMessage>envelope.payload;
                const headers = new Headers();
                for (const h of message.headers) {
                    headers.append(h.key, h.value);
                }
                const response = new Response(message.bodyString, { headers, status: message.statusCode });
                promise.resolve(response);
                return;

            } else {
                return processMessage(envelope);
            }
        }

        const envelope = <WebViewMessageEnvelope>JSON.parse(json);
        try {
            const response = await processRequestOrResponse(envelope);
            if (typeof response !== "undefined") {
                sendMessage(<WebViewMessageEnvelope>{
                    type: envelope.type,
                    messageId: envelope.messageId,
                    payload: response
                });
            }
        }
        catch (err) {
            sendMessage(<WebViewMessageEnvelope>{
                type: "ErrorOccurred",
                messageId: envelope.messageId,
                payload: JSON.stringify(err)
            });
        }
    });
} 

export async function webMessageFetch(url: string, init: RequestInit): Promise<Response> {
    if (init.method?.toUpperCase() === "GET") {
        return await window.fetch(url, init);
    }

    const message: HttpRequestInputMessage = {
        url,
        method: init.method!,
        headers: [],
        bodyString: init.body as string
    };
    (<Headers>init.headers)?.forEach((v, k) => message.headers.push({ key: k, value: v }));

    return await sendMessageAndWaitForResponse<Response>("HttpRequest", message);
}

export function sendPageNotification(methodName: string, args: any[]) {
    sendMessage({
        type: "PageNotification",
        payload: <PageNotificationMessage>{
            methodName: methodName,
            args: args
        }
    });
}

function notifyNavigationCompleted(messageType: string) {
    sendMessage({
        type: messageType,
        payload: <NavigationCompletedMessage>{
            routeName: dotvvm.routeName,
            routeParameters: dotvvm.routeParameters
        }
    });
}

//initCompleted.subscribe(() => {
//     notifyNavigationCompleted("InitCompleted");
//});

//spaNavigated.subscribe(() => {
//    notifyNavigationCompleted("SpaNavigationCompleted");
//});

//spaNavigating.subscribe(() => {
//    sendMessage({
//        type: "SpaNavigating"
//    });
//});
