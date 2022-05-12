﻿using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToroBank.Domain.Entities;

namespace Application.UnitTests.UseCases
{
    public class BuyOrderTest
    {
        private List<Asset> mockAssetBase;
        private List<NegotiatedAssetItem> mockNegotiatedAssets;
        private List<MostNegotiatedAssetItem> mockMostNegotiatedAssetsFroLastSevenDays;
        private List<UserAsset> userAssets;
        private UserAsset assetToSave;
        private decimal price, subtotal, balance;
        private User mockUser;


        public class NegotiatedAssetItem
        {
            public Guid Id { get; set; }
            public int UserId { get; set; }
            public Asset Asset { get; set; }
            public int Quantity { get; set; }
            public DateTime AcquiredAt { get; set; }
        }

        public class Asset
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Value { get; set; }
        }

        public class MostNegotiatedAssetItem
        {
            public Asset Asset { get; set; }
            public int Quantity { get; set; }
        }

       

        [SetUp]
        public void Setup()
        {
            mockUser = new User(123456, "João", "123123456452", 123.24M);
            mockUser.Id = 1;

            mockNegotiatedAssets = new List<NegotiatedAssetItem>();

            var mockAssetBase = new List<Asset>() {
                new Asset{ Id =1, Name = "PETR4", Value = 28.44M },
                new Asset{ Id =2, Name = "MGLU3", Value = 25.91M },
                new Asset{ Id =3, Name = "VVAR3", Value = 25.91M },
                new Asset{ Id =4, Name = "SANB11", Value = 40.77M },
                new Asset{ Id =5, Name = "TORO4", Value = 115.98M },
            };

            //Random values
            Random rndQuantity = new Random(30);
            Random rndUser = new Random(7);
            Random rndAsset = new Random(5);
            Random rndDate = new Random();

            DateTime start = DateTime.Now.AddDays(-7);
            int range = (DateTime.Today - start).Days;

            for (int i = 0; i < 100; i++)
            {
                int assetId = rndAsset.Next(1, 6);
                if (mockAssetBase.Any(f => f.Id == assetId))
                {
                    mockNegotiatedAssets.Add(new NegotiatedAssetItem { Id = Guid.NewGuid(), UserId = rndUser.Next(1, 7), Asset = mockAssetBase.FirstOrDefault(f => f.Id == assetId), Quantity = rndQuantity.Next(1, 30), AcquiredAt = start.AddDays(rndDate.Next(range)) });
                }
            }

            //validation: if one or more assets was not added in list, add a registry
            for (int i = 1; i <= 5; i++)
            {
                if (mockNegotiatedAssets.Count(f => f.Asset.Id == i) == 0)
                {
                    mockNegotiatedAssets.Add(new NegotiatedAssetItem { Id = Guid.NewGuid(), UserId = rndUser.Next(1, 7), Asset = mockAssetBase.FirstOrDefault(f => f.Id == i), Quantity = rndQuantity.Next(1, 30), AcquiredAt = start.AddDays(rndDate.Next(range)) });
                }
            }

            mockMostNegotiatedAssetsFroLastSevenDays = (from x in mockNegotiatedAssets
                                                            .Where(f => f.AcquiredAt >= DateTime.Now.AddDays(-7).Date && f.AcquiredAt <= DateTime.Now.Date)
                                                            .GroupBy(f => f.Asset)
                                         select new MostNegotiatedAssetItem { Asset = x.First().Asset, Quantity = x.Sum(f => f.Quantity) })
                                                            .OrderByDescending(f => f.Quantity).ToList();
            
        }
       

        [Test]
        public void Should_have_only_5_most_negotiated_assets_from_last_7_days()
        {
            Assert.IsNotNull(mockMostNegotiatedAssetsFroLastSevenDays);
            Assert.IsTrue(mockMostNegotiatedAssetsFroLastSevenDays.Count() == 5);
        }

        private List<MostNegotiatedAssetItem> ReturnMostNegotiatedAssetsFromLastDays(int days)
        {
            var mostNegotiatedOrdered = (from x in mockNegotiatedAssets
                                                            .Where(f => f.AcquiredAt >= DateTime.Now.AddDays(-days).Date && f.AcquiredAt <= DateTime.Now.Date)
                                                            .GroupBy(f => f.Asset)
                                         select new MostNegotiatedAssetItem { Asset = x.First().Asset, Quantity = x.Sum(f => f.Quantity) })
                                                            .OrderByDescending(f => f.Quantity).ToList();
            return mostNegotiatedOrdered;
        }

        
        private UserAsset ExecuteUseCase(User user, MostNegotiatedAssetItem selectedAsset, int quantity)
        {
            userAssets = new List<UserAsset>();
            assetToSave = null;

            ////3. Calcular o preço da ação vezes a quantidade e guardar o preço
            
            price = selectedAsset.Asset.Value;
            subtotal = quantity * price;


            ////4. Verificar saldo disponível na conta do usuario
            balance = user.Balance;


            ////5. Se o saldo for menor que R$122.31 - NÃO REALIZAR COMPRA
            if (balance < subtotal)
            {
                //nao realiza compra
                ////5.1. retornar mensagem 'saldo insuficiente' para o usuario
                throw new InvalidOperationException("Saldo insuficiente!");
            }
            else
            {
                ////6. Se o saldo for maior que R$122.31 - REALIZAR COMPRA
                

                //pesquisar ativos
                assetToSave = userAssets.FirstOrDefault(f => f.UserId == user.Id && f.AssetId == selectedAsset.Asset.Id);

                ////6.1. Registrar quantidades de ativos SANB11 ao cliente.

                if (userAssets.Count() == 0)
                {
                    ////6.1.1. Se nao houver ativos, criar e adicionar valor.
                    userAssets.Add(new UserAsset(selectedAsset.Asset.Id, user.Id, quantity));
                    assetToSave = userAssets.FirstOrDefault(f => f.UserId == user.Id && f.AssetId == selectedAsset.Asset.Id);
                }
                else
                {
                    if (assetToSave != null)
                    {
                        ////6.1.2. Atualiza quantidade do ativo existente ao cliente.
                        assetToSave.Quantity += quantity;
                    }
                }

                ////6.2. Debitar o valor do saldo do cliente.
                user.Balance -= subtotal;
            }

            return assetToSave;
        }

        /*No exemplo acima o usuário deseja comprar 3 ações SANB11. Neste caso, a API deve chegar o valor de SANB11 naquele momento (no exemplo, R$40.77), verificar se o usuário tem 
             * pelo menos R$122.31 disponível em conta corrente e, em caso afirmativo, realizar a compra (debitar o saldo e registrar as novas quantidades de ativos SANB11 ao cliente). 
             * Caso não tenha saldo suficiente, ou o ativo informado seja invalido, a API deve retornar uma codido e uma mensagem de erro indicando "saldo insuficiente" ou "ativo invalido". 
             * Esta operação deve impactar o saldo e a lista de ativos do usuário.*/
        [Test]
        public void Should_pass_if_conditions_are_valid()
        {
            var mostNegotiated = mockMostNegotiatedAssetsFroLastSevenDays;
            var selectedAsset = mostNegotiated.FirstOrDefault(a => a.Asset.Name == "SANB11");
            int quantity = 3;

            Assert.DoesNotThrow(() => ExecuteUseCase(mockUser, selectedAsset, quantity));
            Assert.IsNotNull(mockUser);
            Assert.NotNull(assetToSave);
            Assert.GreaterOrEqual(mockUser.Balance, 0);
        }

        //deve passar se não houver erro. OK
        //se houver erro, retornar um InvalidOperationException - OK
        //deve passar se o calculo da compra estiver correto. 
        //se o saldo for acima do minimo para compra, deve registrar novas quantidades de ativos do sanbb ao cliente.
        //se o saldo for acima do minimo para compra, saldo deve ser impactado após a finalização da compra.
        //nao deve passar se o saldo do usuario for menor que o subtotal
        //nao deve passar se o ativo selecionado for invalido: ex: selecionei o SANB11, mas veio TORO

        [Test]
        public void Should_have_error_if_balance_is_insuficient()
        {
            var mostNegotiated = mockMostNegotiatedAssetsFroLastSevenDays;
            var selectedAsset = mostNegotiated.FirstOrDefault(a => a.Asset.Name == "SANB11");
            int quantity = 4;
            
            Assert.Throws<InvalidOperationException>(() => ExecuteUseCase(mockUser, selectedAsset, quantity));
        }
    }
}
