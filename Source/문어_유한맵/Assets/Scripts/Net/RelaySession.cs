using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace OctopusSurvivors
{
    /// <summary>
    /// Unity Relay + Join Code. 서로 다른 집 인터넷에서도 포트 개방 없이 접속한다.
    /// 게임플레이는 건드리지 않고, 연결 수단만 담당한다.
    /// </summary>
    public static class RelaySession
    {
        public const string ConnectionType = "dtls";
        public const float GiveUpSeconds = 30f;

        public static string JoinCode { get; private set; } = string.Empty;
        public static bool UsingRelay { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
        }

        public static void Clear()
        {
            JoinCode = string.Empty;
            UsingRelay = false;
        }

        public static async Task EnsureReadyAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Unity 서비스 초기화 실패.\n" +
                    "Edit > Project Settings > Services 에서 Unity 프로젝트에 로그인한 뒤 이 게임을 연결하세요.\n" +
                    e.Message);
            }

            if (AuthenticationService.Instance.IsSignedIn)
                return;

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                throw new Exception(
                    "익명 로그인 실패.\n" +
                    "https://cloud.unity.com 에서 Authentication 을 켜고 Anonymous Sign-in 을 허용하세요.\n" +
                    e.Message);
            }
        }

        public static async Task<string> HostAsync(UnityTransport transport, int extraPlayers)
        {
            await EnsureReadyAsync();
            var allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, extraPlayers));
            Bind(transport, allocation.ServerEndpoints, allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, allocation.ConnectionData);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            UsingRelay = true;
            return JoinCode;
        }

        public static async Task JoinAsync(UnityTransport transport, string code)
        {
            await EnsureReadyAsync();
            code = Normalize(code);
            if (string.IsNullOrEmpty(code))
                throw new Exception("참가 코드를 입력하세요.");

            var allocation = await RelayService.Instance.JoinAllocationAsync(code);
            Bind(transport, allocation.ServerEndpoints, allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, allocation.HostConnectionData);
            JoinCode = code;
            UsingRelay = true;
        }

        static void Bind(UnityTransport transport, List<RelayServerEndpoint> endpoints,
            byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData)
        {
            RelayServerEndpoint chosen = null;
            if (endpoints != null)
            {
                for (int i = 0; i < endpoints.Count; i++)
                {
                    if (endpoints[i].ConnectionType == ConnectionType)
                    {
                        chosen = endpoints[i];
                        break;
                    }
                }
            }

            if (chosen == null)
                throw new Exception("Relay DTLS 서버 주소를 받지 못했습니다. 인터넷과 Dashboard Relay 설정을 확인하세요.");

            transport.SetRelayServerData(
                chosen.Host, (ushort)chosen.Port, allocationId, key, connectionData, hostConnectionData, true);
        }

        public static string Normalize(string code)
        {
            return string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
        }
    }
}
