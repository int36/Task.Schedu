﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Task.Schedu.Data;
using Task.Schedu.Utility;
using Task.Schedu.Testing.Utils;
using Dapper;
using System.Linq;
using System.Collections.Generic;
using Task.Schedu.Model;

namespace Task.Schedu.Testing
{
    /// <summary>
    /// 订单模块单元测试
    /// </summary>
    [TestClass]
    public class OrderTest : DbAccess<Orders>
    {
        public OrderTest()
        {
            ConfigInit.Init();
        }


        #region 逾期未付款订单关闭
        [TestMethod]
        public void OrderAutoClose()
        {
            var logs = new List<Logs>();
            TaskLog.OrderNoPayCloseLogInfo.WriteLogE("逾期未付款订单关闭开始");
            //待处理订单
            var NoPayOrders = FindBy((client) =>
            {
                Orders orders = null;
                return client.Query<Orders, OrderItems, Orders>(@"SELECT A.Id,A.UserId,A.UserName,A.IntegralDiscount,A.PurseAmount,B.OrderId,B.SkuId,B.Quantity FROM Orders A  
                    Left Join OrderItems B ON A.Id=B.OrderId WHERE OrderStatus=1 And OrderDate<@OrderDate LIMIT 0,100",
                    (order, item) =>
                    {
                        if (orders == null || orders.Id != order.Id)
                            orders = order;
                        if (item != null)
                        {
                            orders.OrderItems.Add(item);
                        }
                        return orders;
                    }, new { OrderDate = DateTime.Now.AddMinutes(-30) }, splitOn: "OrderId");
            }, SysConfig.MainConnect);
            //开始处理
            if (NoPayOrders.Any())
            {
                Commit((client) =>
                {
                    foreach (var item in NoPayOrders)
                    {
                        try
                        {
                            //修改订单状态
                            var flag = client.Execute("UPDATE Orders SET OrderStatusss=5,FinishDate=@FinishDate,CloseReason=@CloseReason WHERE Id=@OrderId",
                                  new
                                  {
                                      OrderId = item.Id,
                                      FinishDate = DateTime.Now,
                                      CloseReason = "逾期未付款,自动关闭"
                                  }) > 0;

                            if (flag)
                            {
                                //返还库存
                                foreach (var items in item.OrderItems)
                                {
                                    client.Execute("UPDATE SKUs SET Stock=Stock+@Quantity,MaxSaleStock=MaxSaleStock+Quantity WHERE Id=@SkuId", new { items.Quantity, items.SkuId });
                                }
                                //返还优惠券
                                //var couponRecord = client.Query("SELECT CounponStatus,UserId,UsedTime,OrderId,CouponPackageId FROM CouponRecord WHERE ")

                                //返还钱包金额
                            }
                        }
                        catch (Exception ex)
                        {
                            logs.Add(new Logs
                            {
                                LogId = Guid.NewGuid().ToString(),
                                CreatedOn = DateTime.Now,
                                LogType = LogType.ERROR,
                                LogMsg = string.Format("订单{0}处理异常,{1}", item.Id, ex.Message)
                            });
                            continue;
                        }
                    }
                    return true;
                }, SysConfig.MainConnect);

                //订单操作异常日志
                if (logs.Any())
                {
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        Commit((client) =>
                        {
                            client.Execute(@"INSERT INTO t_OrderLog(LogId,LogType,LogMsg,CreatedOn) 
                                                 VALUES(@LogId,@LogType,@LogMsg,@CreatedOn)", logs);
                            return true;
                        }, SysConfig.ScheduConnect);
                    });
                }
            }
            TaskLog.OrderNoPayCloseLogInfo.WriteLogE("逾期未付款订单关闭结束");
        }
        #endregion

        #region 逾期未确认订单确认
        [TestMethod]
        public void OrderAutoConfirm()
        {
            TaskLog.OrderConfirmLogInfo.WriteLogE("逾期未确认订单确认开始");
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                TaskLog.OrderConfirmLogInfo.WriteLogE("业务异步处理");
            });
            TaskLog.OrderConfirmLogInfo.WriteLogE("逾期未确认订单确认开始");
        }
        #endregion
    }
}
