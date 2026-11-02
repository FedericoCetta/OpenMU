// <copyright file="GkzPaymentNotificationsController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// MU GKZ: receives the notifications of the payment providers, which grant the bought GKZ Coins.
/// </summary>
/// <remarks>
/// A notification only says that a payment changed: the payment itself is always asked to the API of the
/// provider (see <see cref="GkzMercadoPago.ProcessPaymentAsync"/>), so a forged notification can't grant
/// anything. The provider retries a notification until it gets a 2xx answer, so an error of its API
/// answers 503 to get the notification again later.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/pagos")]
public class GkzPaymentNotificationsController : ControllerBase
{
    private readonly GkzMercadoPago _mercadoPago;
    private readonly ILogger<GkzPaymentNotificationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzPaymentNotificationsController"/> class.
    /// </summary>
    /// <param name="mercadoPago">The client of Mercado Pago.</param>
    /// <param name="logger">The logger.</param>
    public GkzPaymentNotificationsController(GkzMercadoPago mercadoPago, ILogger<GkzPaymentNotificationsController> logger)
    {
        this._mercadoPago = mercadoPago;
        this._logger = logger;
    }

    /// <summary>
    /// Receives a notification (webhook) of Mercado Pago.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 when it's handled or not about a payment; 401 for an invalid signature; 503 to get it again later.</returns>
    [HttpPost("mercadopago")]
    public async Task<IActionResult> MercadoPagoAsync(CancellationToken cancellationToken)
    {
        var type = this.Request.Query["type"].ToString();
        var dataId = this.Request.Query["data.id"].ToString();
        if (type != "payment" || dataId.Length == 0)
        {
            return this.Ok();
        }

        if (!this._mercadoPago.IsValidNotification(this.Request.Headers["x-signature"], this.Request.Headers["x-request-id"], dataId))
        {
            this._logger.LogWarning("Mercado Pago notification for {PaymentId} with an invalid signature.", dataId);
            return this.Unauthorized();
        }

        var result = await this._mercadoPago.ProcessPaymentAsync(dataId, cancellationToken).ConfigureAwait(false);
        return result.Status == GkzPaymentStatus.Error ? this.StatusCode(StatusCodes.Status503ServiceUnavailable) : this.Ok();
    }
}
