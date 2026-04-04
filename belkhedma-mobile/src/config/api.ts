import { Platform } from "react-native";

const defaultHost = Platform.OS === "android" ? "10.0.2.2" : "localhost";

// Can be overridden at runtime:
// EXPO_PUBLIC_API_BASE_URL=http://192.168.1.20:5275 npx expo start
export const API_BASE_URL =
  process.env.EXPO_PUBLIC_API_BASE_URL ?? `http://${defaultHost}:5275`;

