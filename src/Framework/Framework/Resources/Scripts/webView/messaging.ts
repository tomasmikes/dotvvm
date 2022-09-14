type WebViewMessageEnvelope = {
    type: string;
    messageId: number;
    payload: any;
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
type HandlerCommandMessage = {
    action: string;
    content: string;
};

const pendingRequests: { resolve: (result: any) => void, reject: (result: any) => void }[] = [];

// send messages
export function sendMessage(message: any) {
    (window.external as any).sendMessage(message);
}

export async function sendMessageAndWaitForResponse<T>(messageType: string, message: any): Promise<T> {
    const envelope = {
        type: messageType,
        messageId: pendingRequests.length,
        payload: message
    };

    const promise = new Promise<T>((resolve, reject) => {
        pendingRequests[envelope.messageId] = { resolve, reject };
        sendMessage(envelope);
    });
    return await promise;
}

// handle commands from the webview
(window.external as any).receiveMessage(async (json: any) => {

    function processRequestOrResponse(envelope: WebViewMessageEnvelope) {

        if (envelope.type === "HttpRequest") {
            // handle incoming HTTP request responses
            const promise = pendingRequests[envelope.messageId]
            const message = <HttpRequestOutputMessage>envelope.payload;
            const headers = new Headers();
            for (const h of message.headers) {
                headers.append(h.key, h.value);
            }
            const response = new Response(message.bodyString, { headers, status: message.statusCode });
            promise.resolve(response);
            return;

        } else if (envelope.type == "GetViewModelSnapshot") {
            const message: HandlerCommandMessage = {
                action: envelope.type,
                content: JSON.stringify(dotvvm.state)
            };
            return message;
        } else if (envelope.type == "PatchViewModel") {
            dotvvm.patchState(envelope.payload);
            return;
        } else {
            throw `Command ${envelope.type} not found!`;
        }
    }

    const envelope = <WebViewMessageEnvelope>JSON.parse(json);
    try {
        const response = await processRequestOrResponse(envelope);
        if (typeof response !== "undefined") {
            sendMessage({
                type: "HandlerCommand",
                messageId: envelope.messageId,
                payload: response
            });
        }
    }
    catch (err) {
        sendMessage({
            type: "HandlerCommand",
            messageId: envelope.messageId,
            errorMessage: JSON.stringify(err)
        });
    }
});

export async function webMessageFetch(url: string, init: RequestInit): Promise<Response> {
    if (init.method?.toUpperCase() === "GET") {
        return await window.fetch(url, init);
    }

    const message: HttpRequestInputMessage = {
        url,
        method: init.method || "GET",
        headers: [],
        bodyString: init.body as string
    };
    (<Headers>init.headers)?.forEach((v, k) => message.headers.push({ key: k, value: v }));

    return await sendMessageAndWaitForResponse<Response>("HttpRequest", message);
}
