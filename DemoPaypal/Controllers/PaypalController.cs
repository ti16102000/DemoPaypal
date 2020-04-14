﻿using DemoPaypal.Models;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DemoPaypal.Controllers
{
    public class PaypalController : Controller
    {
        // GET: Paypal
        public ActionResult Index()
        {
            return View();
        }
        //work with paypal payment
        private Payment payment;

        //create a payment using an APIContext
        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            var lsItem = new ItemList() { items=new List<Item>()};
            lsItem.items.Add(new Item { name = "Item 1", currency = "USD", price = "5", quantity = "1", sku = "sku" });
            lsItem.items.Add(new Item { name = "Item 2", currency = "USD", price = "5", quantity = "2", sku = "sku" });

            var payer = new Payer() {
                payment_method = "paypal",
                payer_info = new PayerInfo
                {
                    email = "sb-v2sgo1168479@personal.example.com"
                }

            };
            var redictUrl = new RedirectUrls() {
                cancel_url = redirectUrl,
                return_url=redirectUrl
            };
            var detail = new Details() { tax = "1", shipping = "1", subtotal = "15" }; //subtotal : total order, note: sum(price*quantity)
            var amount = new Amount() { currency = "USD", details = detail, total = "17" }; //total= tax + shipping + subtotal
            var transList = new List<Transaction>();
            transList.Add(new Transaction {
                description = "Hotel Management using Paypal",
                invoice_number = Convert.ToString((new Random()).Next(100000)),
                amount = amount,
                item_list=lsItem,
                
            });
            this.payment = new Payment() {
                intent="sale",
                payer=payer,
                transactions=transList,
                redirect_urls=redictUrl
            };
            return this.payment.Create(apiContext);
        }
        //create execute payment method
        private Payment ExecutePayment(APIContext apiContext, string payerID, string paymentID)
        {
            var paymentExecute = new PaymentExecution(){payer_id = payerID};
            this.payment = new Payment() { id = paymentID};
            return this.payment.Execute(apiContext, paymentExecute);
        }
        //create method
        public ActionResult PaymentWithPaypal()
        {
            APIContext apiContext = PaypalConfiguration.GetAPIContext();
            try
            {
                string payerID = Request.Params["PayerID"];
                if (string.IsNullOrEmpty(payerID))
                {
                    //create a payment
                    string baseUri = Request.Url.Scheme + "://" + Request.Url.Authority + "/Paypal/PaymentWithPaypal?guid=";
                    string guid = Convert.ToString((new Random()).Next(100000));
                    var createdPayment = CreatePayment(apiContext, baseUri + guid);

                    var link = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = string.Empty;
                    while (link.MoveNext())
                    {
                        Links links = link.Current;
                        if (links.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = links.href;
                        }
                    }
                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else {
                    var guid = Request.Params["guid"];
                    var executePayment = ExecutePayment(apiContext, payerID, Session[guid] as string);
                    if (executePayment.state.ToLower() != "approved")
                    {
                        return View("Failure");
                    }
                }
            }
            catch (PayPal.PaymentsException ex)
            {
                PaypalLogger.Log("Error: " + ex.Message);
                return View("Failure");
            }
            return View("Success");
        } 
    }
}