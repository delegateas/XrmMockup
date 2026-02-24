using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using DG.Tools.XrmMockup.Database;
using System.ServiceModel;

namespace DG.Tools.XrmMockup
{
    internal sealed class UploadBlockRequestHandler : RequestHandler
    {
        internal UploadBlockRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security)
            : base(core, db, metadata, security, "UploadBlock") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<UploadBlockRequest>(orgRequest);

            var session = core.FileBlockStore.GetUploadSession(request.FileContinuationToken);
            if (session is null)
                throw new FaultException("Invalid or expired file continuation token.");

            var block = new FileBlock
            {
                BlockId = request.BlockId,
                Data = request.BlockData
            };

            session.Blocks.Add(block);

            var resp = new UploadBlockResponse();
            return resp;
        }
    }
}
