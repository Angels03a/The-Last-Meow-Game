using System;

using TLMG.Client.UsersManagerReference;

namespace TLMG.Client.Communication
{
    internal sealed class ModerationConnection : IModerationConnection
    {
        private readonly ServerConnectionManager _connection;
        private readonly ModerationChannel _channel;

        internal ModerationConnection(ServerConnectionManager connection, ModerationChannel channel)
        {
            _connection = connection;
            _channel = channel;
            _channel.Completed += OnCompleted;
        }

        public event EventHandler<ServerEventArgs<OperationResult>> BlockPlayerResultReceived;
        public event EventHandler<ServerEventArgs<OperationResult>> UnblockPlayerResultReceived;
        public event EventHandler<ServerEventArgs<BlockedPlayerList>> BlockedPlayersReceived;
        public event EventHandler<ServerEventArgs<OperationResult>> ReportPlayerResultReceived;
        public event EventHandler<ServerEventArgs<OperationResult>> KickVoteResultReceived;
        public event EventHandler<ServerEventArgs<int>> PlayerExpelled;

        public bool BlockPlayer(int targetUserId)
        {
            bool wasSent = CanModerateRegisteredPlayer(targetUserId)
                && _channel.Send(ModerationOperation.Block, _connection.SessionToken, SendBlock);

            void SendBlock(ModerationManagerClient client, string sessionToken)
            {
                client.BlockPlayer(sessionToken, targetUserId);
            }

            return wasSent;
        }

        public bool UnblockPlayer(int targetUserId)
        {
            bool wasSent = CanModerateRegisteredPlayer(targetUserId)
                && _channel.Send(ModerationOperation.Unblock, _connection.SessionToken, SendUnblock);

            void SendUnblock(ModerationManagerClient client, string sessionToken)
            {
                client.UnblockPlayer(sessionToken, targetUserId);
            }

            return wasSent;
        }

        public bool GetBlockedPlayers()
        {
            bool wasSent = _connection.HasRegisteredSession
                && _channel.Send(ModerationOperation.GetBlockedPlayers, _connection.SessionToken, SendLookup);

            void SendLookup(ModerationManagerClient client, string sessionToken)
            {
                client.GetBlockedPlayers(sessionToken);
            }

            return wasSent;
        }

        public bool ReportPlayer(ReportSubmission submission)
        {
            bool wasSent = _connection.CurrentRoom != null && submission != null
                && CanModerateRegisteredPlayer(submission.TargetUserId)
                && _channel.Send(ModerationOperation.Report, _connection.SessionToken, SendReport);

            void SendReport(ModerationManagerClient client, string sessionToken)
            {
                client.ReportPlayer(sessionToken, submission);
            }

            return wasSent;
        }

        public bool VoteToKick(KickVote vote)
        {
            bool wasSent = _connection.HasSession && _connection.CurrentRoom != null && vote != null
                && vote.RoomId == _connection.CurrentRoom.RoomId
                && vote.TargetUserId != ServerConnectionManager.NoUserId
                && vote.TargetUserId != _connection.PlayerUserId
                && _channel.Send(ModerationOperation.VoteToKick, _connection.SessionToken, SendVote);

            void SendVote(ModerationManagerClient client, string sessionToken)
            {
                client.VoteToKick(sessionToken, vote);
            }

            return wasSent;
        }

        private bool CanModerateRegisteredPlayer(int targetUserId)
        {
            bool isAllowed = _connection.HasRegisteredSession
                && targetUserId > ServerConnectionManager.NoUserId
                && targetUserId != _connection.PlayerUserId;

            return isAllowed;
        }

        internal bool NotifyPlayerExpelled(int roomId)
        {
            EventHandler<ServerEventArgs<int>> handlers = PlayerExpelled;
            bool hasSubscribers = handlers != null;

            if (hasSubscribers)
            {
                handlers(this, new ServerEventArgs<int>(roomId));
            }

            return hasSubscribers;
        }

        private void OnCompleted(ModerationOperation operation, OperationResult result)
        {
            if (_connection.HasSession)
            {
                if (result.IsSuccess && (operation == ModerationOperation.Block
                    || operation == ModerationOperation.Unblock))
                {
                    _connection.RequestFriendList();
                }

                if (operation == ModerationOperation.Block)
                {
                    BlockPlayerResultReceived?.Invoke(this, new ServerEventArgs<OperationResult>(result));
                }
                else if (operation == ModerationOperation.Unblock)
                {
                    UnblockPlayerResultReceived?.Invoke(this, new ServerEventArgs<OperationResult>(result));
                }
                else if (operation == ModerationOperation.GetBlockedPlayers)
                {
                    BlockedPlayerList blockedPlayers = (BlockedPlayerList)result;
                    BlockedPlayersReceived?.Invoke(this, new ServerEventArgs<BlockedPlayerList>(blockedPlayers));
                }
                else if (operation == ModerationOperation.Report)
                {
                    ReportPlayerResultReceived?.Invoke(this, new ServerEventArgs<OperationResult>(result));
                }
                else if (operation == ModerationOperation.VoteToKick)
                {
                    KickVoteResultReceived?.Invoke(this, new ServerEventArgs<OperationResult>(result));
                }
            }
        }
    }
}