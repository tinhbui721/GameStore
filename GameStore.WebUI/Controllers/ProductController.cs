using GameStore.Domain.Infrastructure;
using GameStore.Domain.Model;
using GameStore.WebUI.Areas.Admin.Models.DTO;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GameStore.Domain.Identity;
using GameStore.WebUI.Models;

namespace GameStore.WebUI.Controllers
{
    public class ProductController : Controller
    {
        public ActionResult Console(int code = 0)
        {
            List<ProductDTO> list = GetProductsByCategory(1);
            ViewBag.Title = "Console";
            ViewBag.code = code;
            return View("List", list);
        }
        public ActionResult Accessory(int code = 0)
        {
            List<ProductDTO> list = GetProductsByCategory(2);
            ViewBag.Title = "Accessory";
            ViewBag.code = code;
            return View("List", list);
        }
        public ActionResult Game(int code = 0)
        {
            List<ProductDTO> list = GetProductsByCategory(3);
            ViewBag.Title = "Game";
            ViewBag.code = code;
            return View("List", list);
        }

        private List<ProductDTO> GetProductsByCategory(int categoryid)
        {
            List<ProductDTO> list = new List<ProductDTO>();
            if (System.Web.HttpContext.Current.Cache["ProductList" + categoryid] != null)
            {
                list = (List<ProductDTO>)System.Web.HttpContext.Current.Cache["ProductList" + categoryid];
            }
            else
            {
                using (GameStoreDBContext context = new GameStoreDBContext())
                {
                    var query = from product in context.Products
                                where product.CategoryId == categoryid
                                join category in context.Categories
                                  on product.CategoryId equals category.CategoryId
                                select new ProductDTO { ProductId = product.ProductId, ProductName = product.ProductName, CategoryId = product.CategoryId, CategoryName = category.CategoryName, Price = product.Price, Image = product.Image, Condition = product.Condition, Discount = product.Discount, UserId = product.UserId };
                    list = query.ToList();
                    System.Web.HttpContext.Current.Cache["ProductList" + categoryid] = list;
                }
            }

            return list;
        }

        public ActionResult Search(string productname)
        {
            List<ProductDTO> list = new List<ProductDTO>();

            using (GameStoreDBContext context = new GameStoreDBContext())
            {
                //var query = "Select * from Products where ProductName = '" + productname + "'";
                //list = context.Database.SqlQuery<ProductDTO>(query).ToList();
                if (String.IsNullOrEmpty(productname))
                {
                    var query = from product in context.Products
                                join category in context.Categories
                                  on product.CategoryId equals category.CategoryId
                                select new ProductDTO { ProductId = product.ProductId, ProductName = product.ProductName, CategoryId = product.CategoryId, CategoryName = category.CategoryName, Price = product.Price, Image = product.Image, Condition = product.Condition, Discount = product.Discount, UserId = product.UserId };
                    list = query.ToList();
                }
                else
                {
                    var query = from product in context.Products
                                where product.ProductName.ToLower().Contains(productname.ToLower())
                                join category in context.Categories
                                  on product.CategoryId equals category.CategoryId
                                select new ProductDTO { ProductId = product.ProductId, ProductName = product.ProductName, CategoryId = product.CategoryId, CategoryName = category.CategoryName, Price = product.Price, Image = product.Image, Condition = product.Condition, Discount = product.Discount, UserId = product.UserId };
                    list = query.ToList();
                }
            }
            ViewBag.Title = "Search Result";
            return View("List", list);
        }

        public ActionResult Detail(int id)
        {
            ProductDTO model = null;
            using (GameStoreDBContext context = new GameStoreDBContext())
            {
               //var query = @"Select * from Products where ProductId = '" + id + "'";
               // model = context.Database.SqlQuery<ProductDTO>(query).FirstOrDefault();
                List<Review> model2 = context.Reviews.Where(x => x.ProductId == id).ToList();
                foreach (var item in model2)
                {
                    item.User = new AppUser();
                    item.User.UserName = context.Users.Where(x => x.Id == item.User_Id).FirstOrDefault().UserName;
                }
                var query = from product in context.Products
                            where product.ProductId == id
                            join category in context.Categories
                              on product.CategoryId equals category.CategoryId
                            select new ProductDTO { ProductId = product.ProductId, ProductName = product.ProductName, CategoryId = product.CategoryId, CategoryName = category.CategoryName, Price = product.Price, Image = product.Image, Condition = product.Condition, Discount = product.Discount, UserId = product.UserId };
                model = query.FirstOrDefault();
                model.Reviews = model2;
                //var query2 = from review in context.Reviews where review.ProductId == id select new Review { ReviewId=review.ReviewId,UserId=review.UserId,Comments=review.Comments,ReviewDate=review.ReviewDate };

            }
            return View(model);
        }

        [Authorize(Roles = "Admin, Advanced")]
        public ActionResult MyProducts()
        {
            List<Category> list = new List<Category>();
            using (GameStoreDBContext context = new GameStoreDBContext())
            {
                list = context.Categories.ToList();
            }

            ViewBag.Categories = list;
            List<Category> alllist = new List<Category>(list);
            alllist.Insert(0, new Category { CategoryId = 0, CategoryName = "Select All" });
            ViewBag.CategoryFilter = alllist;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SaveComment()
        {
            string comment = Request["comment"].ToString();
            Review review = new Review();
            review.ProductId = int.Parse(Request["idproduct"].ToString());
            review.Comments = comment;
            review.ReviewDate = DateTime.Now;
            review.User_Id = User.Identity.GetUserId();
            try
            {
                if (ModelState.IsValid)
                {
                    using (GameStoreDBContext context = new GameStoreDBContext())
                    {
                        if (review != null)
                        {

                            context.Reviews.Add(review);
                            context.SaveChanges();
                            return Redirect("/Product/Detail/" + review.ProductId);

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
            }
            return Redirect("/Product/Detail/" + review.ProductId);
        }
        [Authorize(Roles = "Admin, Advanced")]
        public ActionResult MyProductOrders()
        {
            List<ProductOrderDTO> list = new List<ProductOrderDTO>();
            try
            {
                String userid = User.Identity.GetUserId();
                using (GameStoreDBContext context = new GameStoreDBContext())
                {
                    var query = from product in context.Products
                                where product.UserId == userid
                                join category in context.Categories
                                  on product.CategoryId equals category.CategoryId
                                select new ProductOrderDTO { ProductId = product.ProductId, ProductName = product.ProductName, CategoryId = product.CategoryId, CategoryName = category.CategoryName, Price = product.Price, Image = product.Image, Condition = product.Condition, Discount = product.Discount, UserId = product.UserId };
                    list = query.ToList();

                    foreach (ProductOrderDTO product in list)
                    {
                        var orders = from o in context.Orders
                                     join i in context.OrderItems
                                       on o.OrderId equals i.OrderId
                                     join u in context.Users
                                       on o.UserId equals u.Id
                                     where i.ProductId == product.ProductId
                                     orderby o.OrderId descending
                                     select new { o.OrderId, o.UserId, u.UserName, o.FullName, o.Address, o.City, o.State, o.Zip, o.ConfirmationNumber, o.DeliveryDate };
                        product.Orders = orders.Select(o => new OrderDTO { OrderId = o.OrderId, UserId = o.UserId, UserName = o.UserName, FullName = o.FullName, Address = o.Address, City = o.City, State = o.State, Zip = o.Zip, ConfirmationNumber = o.ConfirmationNumber, DeliveryDate = o.DeliveryDate }).ToList();

                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error Occurs:" + ex.Message;
            }

            return View(list);
        }

        public ActionResult AddToCart(CartViewModel value)
        {
            ShoppingCart cart = (ShoppingCart)Session["ShoppingCart"];
            if (cart == null)
            {
                cart = new ShoppingCart();
                Session["ShoppingCart"] = cart;
            }
            using (GameStoreDBContext context = new GameStoreDBContext())
            {
                Product product = context.Products.Find(value.Id);
                if (product != null)
                {
                    if (value.Quantity == 0)
                    {
                        cart.AddItem(value.Id, product);
                        Session["CartCount"] = cart.GetItems().Count();
                        return RedirectToAction(product.Category.CategoryName, new {code = 2});
                    }
                }
            }
            Session["CartCount"] = cart.GetItems().Count();
            return null;
        }
    }
}
