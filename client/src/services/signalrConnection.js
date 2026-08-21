import * as signalR from "@microsoft/signalr"

const HUB_URL = "http://localhost:5235/hubs/agent-status"

export function createAgentStatusConnection() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL)
    .withAutomaticReconnect()
    .build()

  return connection
}
