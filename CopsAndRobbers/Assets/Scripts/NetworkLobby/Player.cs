/* 
 *  Copyright (C) 2021 Deranged Senators
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *      http:www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Me.DerangedSenators.CopsAndRobbers {

    /// <summary>
    /// This Class is designed to host the root player object. It holds information such as the MatchID, TeamID  and is responsible for intitiating the prefab on Game Load
    /// </summary>
    /// @authors Hannah Elliman, Ashwin Jaimal, Nisath Mohamed Nasar, Piotr Krawiec and Hanzalah Ravat
    public class Player : NetworkBehaviour
    {
        public static Player localPlayer;
        [SyncVar] public string MatchId;
        [SyncVar] public int playerIndex;
        [SyncVar] public int teamId;
        public MoneyUpdater MoneyUpdater;

        NetworkMatchChecker networkMatchChecker;

        [SyncVar] public Match currentMatch;
        //[SyncVar] PlayerCameraContoller playerCameraController;

        GameObject playerLobbyUI;

        void Awake()
        {
            networkMatchChecker = GetComponent<NetworkMatchChecker>();

        }

        public override void OnStartClient()
        {
            if (isLocalPlayer)
            {
                localPlayer = this;
            }
            else
            {
                //*Debug.Log($"Spawning other player UI");
                playerLobbyUI = UILobby.instance.SpawnUIPlayerPrefab(this);
            }
        }

        public override void OnStopClient()
        {
            //*Debug.Log($"Client stopped");
            ClientDisconnect();
        }

        public override void OnStopServer()
        {
            //*Debug.Log($"Client stopped on server");
            ServerDisconnect();
        }

        /*
         * HOST GAME
         */

        public void HostGame(bool publicMatch)
        {
            string matchId = MatchMaker.GetRandomMatchId();
            CmdHostGame(matchId, publicMatch);
        }
        
        

        [Command]
        void CmdHostGame(string matchId, bool publicMatch)
        {
            MatchId = matchId;
            if (MatchMaker.instance.HostGame(matchId, gameObject, publicMatch, out playerIndex, out teamId))
            {
                //*Debug.Log($"<color=green>Game hosted successfully</color>");

                networkMatchChecker.matchId = matchId.ToGuid();
                TargetHostGame(true, matchId, playerIndex, teamId);
            }
            else
            {
                //*Debug.Log($"<color=red>Game host failed</color>");
                TargetHostGame(false, matchId, playerIndex, teamId);
            }
        }

        [TargetRpc]
        void TargetHostGame(bool success, string matchId, int playerIndex, int teamId)
        {
            MatchId = matchId;
            this.playerIndex = playerIndex;
            this.teamId = teamId;
            //*Debug.Log($"Match ID: {MatchId} == {matchId}");
            UILobby.instance.HostSuccess(success, matchId);
        }

        /*
         * JOIN GAME
         */

        public void JoinGame(string matchId)
        {
            string matchID = matchId;
            CmdJoinGame(matchID);
        }
        [Command]
        void CmdJoinGame(string matchId)
        {
            MatchId = matchId;
            if (MatchMaker.instance.JoinGame(matchId, gameObject, out playerIndex, out teamId))
            {
                //*Debug.Log($"<color=green>Game Joined successfully</color>");

                networkMatchChecker.matchId = matchId.ToGuid();
                TargetJoinGame(true, matchId, playerIndex, teamId);
            }
            else
            {
                //*Debug.Log($"<color=red>Game Join failed</color>");
                TargetJoinGame(false, matchId, playerIndex, teamId);
            }
        }

        [TargetRpc]
        void TargetJoinGame(bool success, string matchId, int playerIndex, int teamId)
        {
            MatchId = matchId;
            this.playerIndex = playerIndex;
            this.teamId = teamId;
            //*Debug.Log($"Match ID: {MatchId} == {matchId}");
            UILobby.instance.JoinSuccess(success, matchId);
        }

        /*
         * SEARCHING FOR GAME
         */
        public void SearchGame()
        {
            CmdSearchGame();
        }

        [Command]
        void CmdSearchGame()
        {
            if (MatchMaker.instance.SearchGame(gameObject, out playerIndex, out MatchId, out teamId))
            {
                //*Debug.Log($"<color=green>Game Found</color>");

                networkMatchChecker.matchId = MatchId.ToGuid();
                TargetSearchGame(true, MatchId, playerIndex, teamId);
            }
            else
            {
                //*Debug.Log($"<color=red>Game not Found</color>");
                TargetSearchGame(false, MatchId, playerIndex, teamId);
            }
        }

        [TargetRpc]
        void TargetSearchGame(bool success, string matchId, int playerIndex, int teamId)
        {
            this.playerIndex = playerIndex;
            MatchId = matchId;
            this.teamId = teamId;
            //*Debug.Log($"Match ID: {MatchId} == {matchId}");
            UILobby.instance.SearchSuccess(success, matchId);
        }

        /*
         * BEGIN GAME
         */

        public void BeginGame()
        {
            CmdBeginGame();
        }
        [Command]
        void CmdBeginGame()
        {
            MatchMaker.instance.BeginGame(MatchId);
            //*Debug.Log($"<color=yellow>Game Beginning</color>");
        }

        public void StartGame()
        {
            TargetBeginGame();
        }

        [TargetRpc]
        void TargetBeginGame()
        {
            //*Debug.Log($"Match ID: {MatchId} | Beginning");
            //Load in round
            //SceneManager.LoadScene(3, LoadSceneMode.Additive);
            GameObject[] playerPrefabs = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                if (playerPrefabs[i].GetComponents<Player>().Length == 1)
                {
                    if (playerPrefabs[i].GetComponent<Player>().playerIndex == localPlayer.playerIndex)
                    {
                        playerPrefabs[i].GetComponent<SpriteRenderer>().enabled = true;
                        playerPrefabs[i].GetComponent<BoxCollider2D>().enabled = true;
                        playerPrefabs[i].GetComponent<PlayerHealth>().enabled = true;
                        playerPrefabs[i].GetComponent<Animator>().enabled = true;
                        playerPrefabs[i].GetComponent<WeaponManager>().enabled = true;
                        playerPrefabs[i].GetComponent<PlayerMovement>().enabled = true;
                        playerPrefabs[i].GetComponent<NetworkTransform>().enabled = true;
                        playerPrefabs[i].GetComponent<PlayerCameraContoller>().enabled = true;
                        playerPrefabs[i].GetComponent<MoneyUpdater>().enabled = true;
                        playerPrefabs[i].GetComponent<PlayerRespawn>().enabled = true;
                        playerPrefabs[i].GetComponent<BulletDetector>().enabled = true;
                    }
                    playerPrefabs[i].GetComponent<SpriteRenderer>().enabled = true;
                    playerPrefabs[i].GetComponent<BoxCollider2D>().enabled = true;
                    playerPrefabs[i].GetComponent<PlayerHealth>().enabled = true;
                    playerPrefabs[i].GetComponent<Animator>().enabled = true;
                    playerPrefabs[i].GetComponent<PlayerRespawn>().enabled = true;
                    playerPrefabs[i].GetComponent<NetworkTransform>().enabled = true;
                    if (playerPrefabs[i].GetComponent<Player>().teamId == 1) // if team is cops
                    {
                        playerPrefabs[i].layer = 9;
                        string robberLayer = LayerMask.LayerToName(8);
                        //*Debug.Log($"Robber Layer: {robberLayer}");
                        playerPrefabs[i].GetComponent<WeaponManager>().
                                                                        WeaponInventory[0].
                                                                        GetComponent<Melee>().EnemyLayer =
                                                                        1 << LayerMask.NameToLayer("Robbers");
                        Animator anim = playerPrefabs[i].GetComponent<Animator>();
                        playerPrefabs[i].GetComponent<MoneyUpdater>().mTeam = Teams.COPS;
                        anim.runtimeAnimatorController =
                            Resources.Load("Animations/CopAnimations/Player1") as RuntimeAnimatorController;


                    }
                    else if (playerPrefabs[i].GetComponent<Player>().teamId == 2) //if team is robber
                    {
                        playerPrefabs[i].layer = 8;
                        playerPrefabs[i].GetComponent<MoneyUpdater>().mTeam = Teams.ROBBERS;
                        playerPrefabs[i].GetComponent<WeaponManager>().
                                                                        WeaponInventory[0].
                                                                        GetComponent<Melee>().EnemyLayer = 
                                                                        1 << LayerMask.NameToLayer("Cops");
                        Animator anim = playerPrefabs[i].GetComponent<Animator>();

                        anim.runtimeAnimatorController =
                            Resources.Load("Animations/RobberAnimations/RobberAll") as RuntimeAnimatorController;
                    }

                    DontDestroyOnLoad(playerPrefabs[i]);
                }
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }


        /*
         * DISCONNECT GAME
         */

        public void DisconnectGame()
        {
            CmdDisconnectGame();
        }

        [Command]
        void CmdDisconnectGame()
        {
            ServerDisconnect();
        }

        void ServerDisconnect()
        {
            MatchMaker.instance.PlayerDisconnected(this, MatchId);
            networkMatchChecker.matchId = string.Empty.ToGuid();
            RpcDisconnectGame();
        }

        [ClientRpc]
        void RpcDisconnectGame()
        {
            ClientDisconnect();
        }

        void ClientDisconnect()
        {

            //destroy UIPlayer
            if (playerLobbyUI != null)
            {
                Destroy(playerLobbyUI);
            }
        }

        public int GetTeamId()
        {
            return teamId;
        }

        //[Command]
        public void DestroyMoneyBag(GameObject mb) {
            //*Debug.Log("attempting to destroy game object on network");
            NetworkManager.Destroy(mb);
            //NetworkServer.Destroy(mb);
            //Destroy(mb);
        }
        
        [Command]
        public void CmdDestroyBullet(GameObject gameObject)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}

































